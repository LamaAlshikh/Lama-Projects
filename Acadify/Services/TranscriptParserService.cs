using System.Text;
using System.Text.RegularExpressions;
using Acadify.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Acadify.Services
{
    public class TranscriptParserService : ITranscriptParserService
    {
        private static readonly HashSet<string> PassedGrades = new(StringComparer.OrdinalIgnoreCase)
        {
            "A+", "A", "B+", "B", "C+", "C", "D+", "D", "P"
        };

        private static readonly HashSet<string> KnownGrades = new(StringComparer.OrdinalIgnoreCase)
        {
            "A+", "A", "B+", "B", "C+", "C", "D+", "D", "F", "NP", "P"
        };

        public async Task<List<TranscriptCourseItem>> ParseTranscriptAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return new List<TranscriptCourseItem>();

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            return ExtractCoursesFromPdf(memoryStream);
        }

        private List<TranscriptCourseItem> ExtractCoursesFromPdf(Stream pdfStream)
        {
            var results = new Dictionary<string, TranscriptCourseItem>(StringComparer.OrdinalIgnoreCase);

            using var document = PdfDocument.Open(pdfStream);

            foreach (var page in document.GetPages())
            {
                var letters = page.Letters?.ToList() ?? new List<Letter>();
                if (!letters.Any())
                    continue;

                var columnGroups = SplitIntoColumns(letters);

                foreach (var columnLetters in columnGroups)
                {
                    var lines = BuildLinesFromLetters(columnLetters);

                    foreach (var item in ExtractCoursesFromLines(lines))
                    {
                        if (results.TryGetValue(item.CourseId, out var existing))
                        {
                            if (!existing.IsPassed && item.IsPassed)
                            {
                                results[item.CourseId] = item;
                            }
                            else if (string.IsNullOrWhiteSpace(existing.Grade) && !string.IsNullOrWhiteSpace(item.Grade))
                            {
                                results[item.CourseId] = item;
                            }
                        }
                        else
                        {
                            results[item.CourseId] = item;
                        }
                    }
                }
            }

            return results.Values
                .OrderBy(x => x.CourseId)
                .ToList();
        }

        private List<List<Letter>> SplitIntoColumns(List<Letter> letters)
        {
            var results = new List<List<Letter>>();

            if (letters == null || letters.Count == 0)
                return results;

            var minX = letters.Min(l => l.GlyphRectangle.Left);
            var maxX = letters.Max(l => l.GlyphRectangle.Right);
            var middleX = (minX + maxX) / 2.0;

            var left = letters
                .Where(l => ((l.GlyphRectangle.Left + l.GlyphRectangle.Right) / 2.0) <= middleX)
                .ToList();

            var right = letters
                .Where(l => ((l.GlyphRectangle.Left + l.GlyphRectangle.Right) / 2.0) > middleX)
                .ToList();

            if (left.Any())
                results.Add(left);

            if (right.Any())
                results.Add(right);

            return results;
        }

        private List<string> BuildLinesFromLetters(List<Letter> letters)
        {
            var results = new List<string>();

            if (letters == null || letters.Count == 0)
                return results;

            var ordered = letters
                .OrderByDescending(l => GetY(l))
                .ThenBy(l => l.GlyphRectangle.Left)
                .ToList();

            var groups = new List<List<Letter>>();
            const double yTolerance = 2.5;

            foreach (var letter in ordered)
            {
                var y = GetY(letter);

                var existingGroup = groups.FirstOrDefault(g => Math.Abs(GetY(g[0]) - y) <= yTolerance);

                if (existingGroup == null)
                {
                    groups.Add(new List<Letter> { letter });
                }
                else
                {
                    existingGroup.Add(letter);
                }
            }

            foreach (var group in groups.OrderByDescending(g => GetY(g[0])))
            {
                var line = BuildLineText(group);
                if (!string.IsNullOrWhiteSpace(line))
                    results.Add(line);
            }

            return results;
        }

        private string BuildLineText(List<Letter> letters)
        {
            if (letters == null || letters.Count == 0)
                return string.Empty;

            var ordered = letters
                .OrderBy(l => l.GlyphRectangle.Left)
                .ToList();

            var avgWidth = ordered.Average(l => l.GlyphRectangle.Width);
            var gapThreshold = Math.Max(avgWidth * 1.2, 3.0);

            var sb = new StringBuilder();
            double? previousRight = null;

            foreach (var letter in ordered)
            {
                if (previousRight.HasValue)
                {
                    var gap = letter.GlyphRectangle.Left - previousRight.Value;

                    if (gap > gapThreshold)
                        sb.Append(' ');
                }

                sb.Append(letter.Value);
                previousRight = letter.GlyphRectangle.Right;
            }

            return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
        }

        private List<TranscriptCourseItem> ExtractCoursesFromLines(List<string> lines)
        {
            var results = new List<TranscriptCourseItem>();

            if (lines == null || lines.Count == 0)
                return results;

            var startRegex = new Regex(@"^(?<prefix>[A-Z]{4})\s+(?<number>\d{3})\b", RegexOptions.IgnoreCase);
            var endGradeRegex = new Regex(@"(?<grade>A\+|A|B\+|B|C\+|C|D\+|D|F|NP|P)\s*$", RegexOptions.IgnoreCase);

            foreach (var rawLine in lines)
            {
                var line = NormalizeLine(rawLine);

                if (ShouldIgnoreLine(line))
                    continue;

                var startMatch = startRegex.Match(line);
                if (!startMatch.Success)
                    continue;

                var gradeMatch = endGradeRegex.Match(line);
                if (!gradeMatch.Success)
                    continue;

                var prefix = startMatch.Groups["prefix"].Value.ToUpper();
                var number = startMatch.Groups["number"].Value;
                var grade = NormalizeGrade(gradeMatch.Groups["grade"].Value);

                if (!KnownGrades.Contains(grade))
                    continue;

                results.Add(new TranscriptCourseItem
                {
                    CourseId = $"{prefix}-{number}",
                    Grade = grade,
                    IsPassed = PassedGrades.Contains(grade)
                });
            }

            return results;
        }

        private string NormalizeLine(string? line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return string.Empty;

            return Regex.Replace(line.Trim(), @"\s+", " ");
        }

        private string NormalizeGrade(string? grade)
        {
            if (string.IsNullOrWhiteSpace(grade))
                return string.Empty;

            return grade.Trim().ToUpper().Replace(" ", "");
        }

        private bool ShouldIgnoreLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return true;

            var ignoredPhrases = new[]
            {
                "STUDENT INFORMATION",
                "STUDENT NAME",
                "STUDENT NO",
                "COLLEGE",
                "MAJOR",
                "STATUS",
                "SUBJECT CODE",
                "COURSE DESCRIPTION",
                "MARKS",
                "LETTER",
                "TERM",
                "CUMULATIVE",
                "TRANSFER EQUIVALENCY",
                "QUARTER SYSTEM STARTS",
                "SEMESTER SYSTEM RE-ADOPTED",
                "PRINT DATE",
                "PAGE:",
                "GPA",
                "END OF TRANSCRIPT",
                "DEAN OF ADMISSION"
            };

            return ignoredPhrases.Any(p => line.Contains(p, StringComparison.OrdinalIgnoreCase));
        }

        private double GetY(Letter letter)
        {
            return letter.GlyphRectangle.Bottom;
        }
    }
}