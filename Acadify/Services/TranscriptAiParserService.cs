using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Acadify.Models;
using UglyToad.PdfPig;

namespace Acadify.Services
{
    public class TranscriptAiParserService : ITranscriptAiParserService
    {
        private readonly AiAcademicAgentService _aiSummaryService;

        // الدرجات التي تعتبر ناجحة / منجزة
        // حسب متطلبات النظام: NP و TR تعتبر ناجحة
        private static readonly HashSet<string> PassedGrades = new(StringComparer.OrdinalIgnoreCase)
        {
            "A+", "A",
            "B+", "B",
            "C+", "C",
            "D+", "D",
            "P",
            "NP",
            "TR"
        };

        // جميع الدرجات المتوقعة في الترانسكربت
        // هذه القائمة تساعد الـ parser يعرف أن النص يمثل Grade
        private static readonly HashSet<string> KnownGrades = new(StringComparer.OrdinalIgnoreCase)
        {
            "A+", "A",
            "B+", "B",
            "C+", "C",
            "D+", "D",
            "F",
            "P",
            "NP",
            "TR",
            "W",
            "WF",
            "WP",
            "IP",
            "I",
            "IC"
        };

        public TranscriptAiParserService(AiAcademicAgentService aiSummaryService)
        {
            _aiSummaryService = aiSummaryService;
        }

        public async Task<List<TranscriptCourseItem>> ParseTranscriptAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return new List<TranscriptCourseItem>();

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            string rawText;

            try
            {
                rawText = ExtractPdfText(memoryStream);
            }
            catch
            {
                return new List<TranscriptCourseItem>();
            }

            if (string.IsNullOrWhiteSpace(rawText))
                return new List<TranscriptCourseItem>();

            var cleanedText = CleanTranscriptText(rawText);

            if (string.IsNullOrWhiteSpace(cleanedText))
                return new List<TranscriptCourseItem>();

            string aiResponse = string.Empty;

            try
            {
                string prompt = BuildPrompt(cleanedText);

                aiResponse = await _aiSummaryService.GetRawResponseAsync(
                    prompt,
                    "You extract structured course data from university transcripts and return clean JSON only.",
                    1600);
            }
            catch
            {
                aiResponse = string.Empty;
            }

            /*
            // استخراج المواد من AI
            var aiCourses = NormalizeAndFilterCourses(ParseJsonResponse(aiResponse));

            // استخراج المواد بالـ Regex كخطة احتياطية
            var regexCourses = NormalizeAndFilterCourses(ParseCoursesFromRawText(cleanedText));

            // دمج نتائج AI و Regex حتى لا نخسر مادة لو أحدهما قرأها بشكل ناقص
            return MergeParsedCourses(aiCourses, regexCourses);
            */



            // استخراج المواد من AI
            var aiCourses = NormalizeAndFilterCourses(ParseJsonResponse(aiResponse));

            // استخراج المواد بالـ Regex كخطة احتياطية
            var regexCourses = NormalizeAndFilterCourses(ParseCoursesFromRawText(cleanedText));



            // دمج نتائج AI و Regex حتى لا نخسر مادة لو أحدهما قرأها بشكل ناقص
            var finalCourses = MergeParsedCourses(aiCourses, regexCourses);

            // Fallback:
            // بعض ملفات الترانسكربت تطلع النص ملخبط، فتظهر أسماء المواد بدون كود واضح.
            // لذلك نضيف مواد معروفة من الخطة إذا وجدنا أسماءها في النص الخام.
            // finalCourses = AddKnownPassedCoursesFromText(finalCourses, cleanedText);


            // Debug: حفظ المواد التي قرأها النظام فعليًا
            // هذا الملف يوضح CourseId + Grade + IsPassed لكل مادة
            var debugCoursesPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "debug_parsed_courses.txt"
            );

            Directory.CreateDirectory(Path.GetDirectoryName(debugCoursesPath)!);

            var debugLines = finalCourses
                .Select(c => $"{c.CourseId} | Grade: {c.Grade} | IsPassed: {c.IsPassed}");

            File.WriteAllLines(debugCoursesPath, debugLines);

            // إرجاع المواد النهائية بعد الدمج والتنظيف
            return finalCourses;
        }
        /*
                private string ExtractPdfText(Stream pdfStream)
                {
                    var sb = new StringBuilder();

                    using var document = PdfDocument.Open(pdfStream);

                    foreach (var page in document.GetPages())
                    {
                        sb.AppendLine(page.Text);
                    }

                    return sb.ToString();
                }
        */


        private string ExtractPdfText(Stream pdfStream)
        {
            var sb = new StringBuilder();

            using var document = PdfDocument.Open(pdfStream);

            int pageNumber = 1;

            foreach (var page in document.GetPages())
            {
                // نقرأ الكلمات مع مواقعها بدل page.Text
                var words = page.GetWords()
                    .Where(w => !string.IsNullOrWhiteSpace(w.Text))
                    .Select(w => new PdfWordBox
                    {
                        Text = w.Text.Trim(),
                        X = w.BoundingBox.Left,
                        Y = w.BoundingBox.Bottom
                    })
                    .OrderByDescending(w => w.Y)
                    .ThenBy(w => w.X)
                    .ToList();

                var lines = GroupWordsIntoLines(words);

                sb.AppendLine($"========== PAGE {pageNumber} ==========");

                foreach (var line in lines)
                {
                    var lineText = string.Join(" ", line
                        .OrderBy(w => w.X)
                        .Select(w => w.Text));

                    sb.AppendLine(lineText);
                }

                sb.AppendLine();
                pageNumber++;
            }

            return sb.ToString();
        }

        private static List<List<PdfWordBox>> GroupWordsIntoLines(List<PdfWordBox> words)
        {
            var lines = new List<List<PdfWordBox>>();

            // كل ما زاد الرقم، زاد التسامح في تجميع الكلمات في نفس السطر
            const double lineTolerance = 3.0;

            foreach (var word in words)
            {
                var existingLine = lines.FirstOrDefault(line =>
                    Math.Abs(line.Average(w => w.Y) - word.Y) <= lineTolerance);

                if (existingLine == null)
                {
                    lines.Add(new List<PdfWordBox> { word });
                }
                else
                {
                    existingLine.Add(word);
                }
            }

            return lines
                .OrderByDescending(line => line.Average(w => w.Y))
                .ToList();
        }

        private class PdfWordBox
        {
            public string Text { get; set; } = "";
            public double X { get; set; }
            public double Y { get; set; }
        }

        private string CleanTranscriptText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var lines = text
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => Regex.Replace(line, @"\s+", " ").Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line));

            return string.Join(Environment.NewLine, lines);
        }

        private string BuildPrompt(string transcriptText)
        {
            // تقليل النص إذا كان طويل جدًا
            if (transcriptText.Length > 30000)
                transcriptText = transcriptText[..30000];

            return
                "You are reading a university transcript for a student in the Information Systems department.\n\n" +
                "Extract only actual course records from the transcript.\n\n" +
                "For each course, return:\n" +
                "- courseId\n" +
                "- grade\n" +
                "- isPassed\n\n" +
                "Rules:\n" +
                "1. Normalize course IDs to this format: ABCD-123.\n" +
                "2. If the transcript shows codes like \"CPIS 342\", convert them to \"CPIS-342\".\n" +
                "3. Passed grades are: A+, A, B+, B, C+, C, D+, D, P, NP, TR.\n" +
                "4. Not passed grades include: F, W, WF, WP, IP, I, IC.\n" +
                "5. Treat NP and TR as passed according to this system's rule.\n" +
                "6. Ignore GPA, totals, semester labels, page headers, student info, signatures, and non-course text.\n" +
                "7. Return valid JSON array only, with no markdown fences and no explanation.\n" +
                "8. If a course appears multiple times, return the latest meaningful record if possible.\n" +
                "9. Output format exactly like this:\n" +
                "[\n" +
                "  {\n" +
                "    \"courseId\": \"CPIS-342\",\n" +
                "    \"grade\": \"A+\",\n" +
                "    \"isPassed\": true\n" +
                "  }\n" +
                "]\n\n" +
                "Transcript text:\n" +
                transcriptText;
        }

        private List<TranscriptCourseItem> ParseJsonResponse(string aiResponse)
        {
            if (string.IsNullOrWhiteSpace(aiResponse))
                return new List<TranscriptCourseItem>();

            var candidates = ExtractJsonCandidates(aiResponse);

            foreach (var candidate in candidates)
            {
                var directList = TryDeserializeList(candidate);

                if (directList.Any())
                    return directList;

                var wrappedList = TryDeserializeWrappedList(candidate);

                if (wrappedList.Any())
                    return wrappedList;
            }

            return new List<TranscriptCourseItem>();
        }

        private List<string> ExtractJsonCandidates(string aiResponse)
        {
            var results = new List<string>();

            if (string.IsNullOrWhiteSpace(aiResponse))
                return results;

            var trimmed = aiResponse.Trim();

            results.Add(trimmed);

            // إزالة markdown fences مثل ```json
            var noFence = trimmed
                .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
                .Replace("```", "")
                .Trim();

            if (!string.IsNullOrWhiteSpace(noFence) && !results.Contains(noFence))
                results.Add(noFence);

            // استخراج أول JSON array
            int firstBracket = noFence.IndexOf('[');
            int lastBracket = noFence.LastIndexOf(']');

            if (firstBracket >= 0 && lastBracket > firstBracket)
            {
                var arrayOnly = noFence.Substring(firstBracket, lastBracket - firstBracket + 1).Trim();

                if (!results.Contains(arrayOnly))
                    results.Add(arrayOnly);
            }

            // استخراج أول JSON object
            int firstBrace = noFence.IndexOf('{');
            int lastBrace = noFence.LastIndexOf('}');

            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                var objectOnly = noFence.Substring(firstBrace, lastBrace - firstBrace + 1).Trim();

                if (!results.Contains(objectOnly))
                    results.Add(objectOnly);
            }

            return results;
        }

        private List<TranscriptCourseItem> TryDeserializeList(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = JsonSerializer.Deserialize<List<TranscriptCourseItem>>(json, options);

                return result ?? new List<TranscriptCourseItem>();
            }
            catch
            {
                return new List<TranscriptCourseItem>();
            }
        }

        private List<TranscriptCourseItem> TryDeserializeWrappedList(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    return new List<TranscriptCourseItem>();

                foreach (var propName in new[] { "courses", "items", "data", "result" })
                {
                    if (doc.RootElement.TryGetProperty(propName, out var prop) &&
                        prop.ValueKind == JsonValueKind.Array)
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };

                        var result = JsonSerializer.Deserialize<List<TranscriptCourseItem>>(prop.GetRawText(), options);

                        return result ?? new List<TranscriptCourseItem>();
                    }
                }

                return new List<TranscriptCourseItem>();
            }
            catch
            {
                return new List<TranscriptCourseItem>();
            }
        }
        private List<TranscriptCourseItem> ParseCoursesFromRawText(string text)
        {
            var result = new Dictionary<string, TranscriptCourseItem>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(text))
                return result.Values.ToList();

            var lines = text
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => Regex.Replace(line, @"\s+", " ").Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            var courseRegex = new Regex(
                @"\b([A-Z]{3,4})\s*-?\s*(\d{3})\b",
                RegexOptions.IgnoreCase
            );

            // الترتيب مهم: القريدات الطويلة قبل القصيرة
            var gradeRegex = new Regex(
                @"(?<![A-Z])(A\+|B\+|C\+|D\+|NP|TR|WF|WP|IP|IC|P|W|F|A|B|C|D|I)(?![A-Z])",
                RegexOptions.IgnoreCase
            );

            for (int i = 0; i < lines.Count; i++)
            {
                var currentLine = lines[i];

                var courseMatches = courseRegex.Matches(currentLine);

                foreach (Match courseMatch in courseMatches)
                {
                    if (!courseMatch.Success)
                        continue;

                    var courseId = NormalizeCourseId(
                        $"{courseMatch.Groups[1].Value}-{courseMatch.Groups[2].Value}"
                    );

                    if (!IsValidCourseId(courseId))
                        continue;

                    // نبحث عن القريد في نفس السطر وبعده بسطرين
                    var window = currentLine;

                    if (i + 1 < lines.Count)
                        window += " " + lines[i + 1];

                    if (i + 2 < lines.Count)
                        window += " " + lines[i + 2];

                    // إزالة أرقام الرومان من أسماء المواد مثل Programming (I)
                    // حتى لا تنفهم I كقريد
                    window = Regex.Replace(window, @"\((I|II|III|IV|V)\)", "", RegexOptions.IgnoreCase);

                    string grade = "";

                    // نبدأ البحث بعد مكان كود المادة في السطر قدر الإمكان
                    var afterCoursePart = currentLine.Length > courseMatch.Index + courseMatch.Length
                        ? currentLine.Substring(courseMatch.Index + courseMatch.Length)
                        : "";

                    if (i + 1 < lines.Count)
                        afterCoursePart += " " + lines[i + 1];

                    if (i + 2 < lines.Count)
                        afterCoursePart += " " + lines[i + 2];

                    afterCoursePart = Regex.Replace(afterCoursePart, @"\((I|II|III|IV|V)\)", "", RegexOptions.IgnoreCase);

                    var gradeMatch = gradeRegex.Match(afterCoursePart);

                    if (!gradeMatch.Success)
                        gradeMatch = gradeRegex.Match(window);

                    if (gradeMatch.Success)
                        grade = NormalizeGrade(gradeMatch.Groups[1].Value);

                    // إذا ما وجدنا قريد، لا نضيف المادة حتى لا نحسبها غلط
                    if (string.IsNullOrWhiteSpace(grade))
                        continue;

                    var item = new TranscriptCourseItem
                    {
                        CourseId = courseId,
                        Grade = grade,
                        IsPassed = IsPassingGrade(grade)
                    };

                    if (result.TryGetValue(courseId, out var existing))
                    {
                        // إذا نفس المادة ظهرت أكثر من مرة، نحتفظ بالنسخة المجتازة
                        if (!existing.IsPassed && item.IsPassed)
                            result[courseId] = item;
                    }
                    else
                    {
                        result[courseId] = item;
                    }
                }
            }

            return result.Values
                .OrderBy(x => x.CourseId)
                .ToList();
        }

        private List<TranscriptCourseItem> NormalizeAndFilterCourses(IEnumerable<TranscriptCourseItem> courses)
        {
            return courses
                // استبعاد العناصر الفارغة
                .Where(x => x != null)

                // توحيد كود المادة والقريد وتحديد IsPassed
                .Select(x =>
                {
                    var normalizedCourseId = NormalizeCourseId(x.CourseId);
                    var normalizedGrade = NormalizeGrade(x.Grade);

                    return new TranscriptCourseItem
                    {
                        // كود المادة بعد إزالة الشرطة والمسافات
                        CourseId = normalizedCourseId,

                        // القريد بعد التوحيد
                        Grade = normalizedGrade,

                        // إذا كان IsPassed صحيح من AI أو القريد من درجات النجاح
                        IsPassed = x.IsPassed || IsPassingGrade(normalizedGrade)
                    };
                })

                // قبول المواد التي شكل كودها صحيح فقط
                .Where(x => IsValidCourseId(x.CourseId))

                // قبول المواد التي لها قريد معروف فقط
                .Where(x => !string.IsNullOrWhiteSpace(x.Grade))
                .Where(x => KnownGrades.Contains(x.Grade))

                // إزالة التكرار
                // إذا نفس المادة ظهرت أكثر من مرة، نختار النسخة المجتازة إن وجدت
                .GroupBy(x => x.CourseId)
                .Select(g =>
                {
                    var passed = g.FirstOrDefault(x => x.IsPassed);
                    return passed ?? g.First();
                })

                .OrderBy(x => x.CourseId)
                .ToList();
        }

        private List<TranscriptCourseItem> MergeParsedCourses(
            List<TranscriptCourseItem> aiCourses,
            List<TranscriptCourseItem> regexCourses)
        {
            // دمج نتائج AI و Regex في قائمة واحدة
            var allCourses = new List<TranscriptCourseItem>();

            if (aiCourses != null)
                allCourses.AddRange(aiCourses);

            if (regexCourses != null)
                allCourses.AddRange(regexCourses);

            return allCourses
                // استبعاد أي مادة بدون كود
                .Where(x => !string.IsNullOrWhiteSpace(x.CourseId))

                // إزالة التكرار
                // إذا نفس المادة ظهرت من AI و Regex، نختار النسخة المجتازة
                .GroupBy(x => x.CourseId)
                .Select(g =>
                {
                    var passed = g.FirstOrDefault(x => x.IsPassed);
                    return passed ?? g.First();
                })

                .OrderBy(x => x.CourseId)
                .ToList();
        }

        /*
        private static List<TranscriptCourseItem> AddKnownPassedCoursesFromText(
    List<TranscriptCourseItem> courses,
    string transcriptText)
        {
            // تحويل النص لحروف كبيرة لتسهيل البحث
            var upperText = (transcriptText ?? "").ToUpperInvariant();

            // نسخة بدون مسافات تساعدنا نلقط الأكواد الملتصقة مثل 498401
            var compactText = Regex.Replace(upperText, @"\s+", "");

            // تحويل القائمة إلى Dictionary عشان نعدل أو نضيف بسهولة
            var coursesById = courses
                .Where(c => !string.IsNullOrWhiteSpace(c.CourseId))
                .GroupBy(c => NormalizeCourseId(c.CourseId))
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(c => c.IsPassed).First(),
                    StringComparer.OrdinalIgnoreCase
                );

            // دالة داخلية:
            // إذا لقينا اسم المادة في النص، نضيفها أو نحدّثها كمنجزة
            void AddOrMarkPassed(string courseId, string grade, params string[] keywords)
            {
                bool found = keywords.Any(k =>
                    upperText.Contains(k.ToUpperInvariant()) ||
                    compactText.Contains(Regex.Replace(k.ToUpperInvariant(), @"\s+", ""))
                );

                if (!found)
                    return;

                var normalizedCourseId = NormalizeCourseId(courseId);
                var normalizedGrade = NormalizeGrade(grade);

                if (coursesById.TryGetValue(normalizedCourseId, out var existingCourse))
                {
                    // إذا المادة موجودة مسبقًا، نؤكد أنها مجتازة
                    existingCourse.Grade = string.IsNullOrWhiteSpace(existingCourse.Grade)
                        ? normalizedGrade
                        : NormalizeGrade(existingCourse.Grade);

                    existingCourse.IsPassed = true;
                }
                else
                {
                    // إذا المادة غير موجودة، نضيفها كمنجزة
                    coursesById[normalizedCourseId] = new TranscriptCourseItem
                    {
                        CourseId = normalizedCourseId,
                        Grade = normalizedGrade,
                        IsPassed = true
                    };
                }
            }

            // مواد ظهرت في الترانسكربت بأسمائها، لكن الـ Regex قد لا يلتقط كودها بسبب لخبطة الـ PDF
            AddOrMarkPassed("ARAB101", "TR", "ARABIC LANGUAGE (1)", "LANGUAGE SKILLS");
            AddOrMarkPassed("ARAB201", "TR", "ARABIC LANGUAGE (2)", "WRITING SKILLS");

            AddOrMarkPassed("BUS232", "TR", "MANAGEMENT OF ORGANIZATIONS", "MODERN BUSINESS MODELS");

            AddOrMarkPassed("CPIS312", "A", "INFORMATION & COMPUTER SECURITY", "INFORMATION & COMPUTER SECURIT");
            AddOrMarkPassed("CPIS352", "A", "APPLICATIONS DESIGN & DEVEL", "IS APPLICATIONS DESIGN");
            AddOrMarkPassed("CPIS357", "A", "SOFTWARE QUALITY AND TESTING");
            AddOrMarkPassed("CPIS358", "A", "INTERNET APPLICATIONS", "WEB PRO");
            AddOrMarkPassed("CPIS380", "A", "E-BUSINESS SYS", "INTRODUCTION TO E-BUSINESS");

            AddOrMarkPassed("CPIS323", "NP", "SUMMER(WORKPLACE)TRAINING", "SUMMER WORKPLACE", "WORKPLACE TRAINING");
            AddOrMarkPassed("CPIS342", "A", "DATA WAREHOUSING AND MINING", "DATA MINING");
            AddOrMarkPassed("CPIS428", "A", "PROFESSIONAL COMPUTING ISSUES");

            // Senior Project I فقط، لأنه CPIS499 مطلوب يكون توصية وليس مادة منجزة
            AddOrMarkPassed("CPIS498", "A", "SENIOR PROJECT I");

            // في النص المضغوط ظهر التسلسل 498401، وهذا غالبًا يعني CPIS498 ثم ISLS401
            AddOrMarkPassed("ISLS401", "A", "ISLS401", "ISLS-401", "ISLAMIC CULTURE (IV)", "498401");

            return coursesById.Values
                .OrderBy(c => c.CourseId)
                .ToList();
        }*/

        private static string NormalizeCourseId(string? courseId)
        {
            // توحيد كود المادة بإزالة الرموز والمسافات وتحويله لحروف كبيرة
            // مثال: CPIS-210 / CPIS 210 / cpis_210 تصبح CPIS210
            return (courseId ?? "")
                .Replace("-", "")
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("/", "")
                .Trim()
                .ToUpper();
        }

        private static bool IsValidCourseId(string? courseId)
        {
            // توحيد كود المادة قبل التحقق
            var normalizedCourseId = NormalizeCourseId(courseId);

            // رفض الكود الفاضي
            if (string.IsNullOrWhiteSpace(normalizedCourseId))
                return false;

            // يقبل مثل:
            // CPIS210, CPCS202, ARAB101, BUS232, ACCT333
            return Regex.IsMatch(normalizedCourseId, @"^[A-Z]{3,4}\d{3}$");
        }

        private static string NormalizeGrade(string? grade)
        {
            // توحيد شكل القريد قبل المقارنة
            // مثال: " tr " أو "n p" تتحول إلى "TR" و "NP"
            return (grade ?? "")
                .Trim()
                .Replace(" ", "")
                .ToUpper();
        }

        private static bool IsPassingGrade(string? grade)
        {
            // تحديد هل القريد يعتبر ناجح حسب قائمة PassedGrades
            return PassedGrades.Contains(NormalizeGrade(grade));
        }
    }
}