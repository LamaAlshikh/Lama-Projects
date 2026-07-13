using System.Text.RegularExpressions;
using Acadify.Models;
using Microsoft.EntityFrameworkCore;
using Db = Acadify.Models.Db;

namespace Acadify.Services
{
    public class RecommendationEngineService : IRecommendationEngineService
    {
        private readonly Db.AcadifyDbContext _context;

        public RecommendationEngineService(Db.AcadifyDbContext context)
        {
            _context = context;
        }

        public async Task<List<RecommendedCourseVm>> GenerateRecommendationsAsync(
            int planId,
            List<TranscriptCourseItem> transcriptCourses)
        {
            transcriptCourses ??= new List<TranscriptCourseItem>();

            var passedCourseIds = transcriptCourses
                // الاعتماد فقط على المواد التي حددها الـ parser كمواد مجتازة
                .Where(x => x.IsPassed)
                .Select(x => NormalizeCourseId(x.CourseId))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);


            var planCourses = await _context.Set<Db.StudyPlanCourse>()
                .Where(sp => sp.PlanId == planId)
                .Join(
                    _context.Set<Db.Course>(),
                    sp => sp.CourseId,
                    c => c.CourseId,
                    (sp, c) => new
                    {
                        CourseId = c.CourseId,
                        CourseName = c.CourseName,
                        Hours = c.Hours,
                        Prerequisite = c.Prerequisite,
                        SemesterNo = sp.SemesterNo,
                        DisplayOrder = sp.DisplayOrder
                    })
                .Where(x => x.Hours > 0)
                .OrderBy(x => x.SemesterNo)
                .ThenBy(x => x.DisplayOrder)
                .ToListAsync();

            var remaining = planCourses
                .Where(c => !passedCourseIds.Contains(NormalizeCourseId(c.CourseId)))
                .ToList();

            if (!remaining.Any())
                return new List<RecommendedCourseVm>();

            var firstIncompleteSemester = remaining
                .Where(x => x.SemesterNo != 81)
                .Select(x => x.SemesterNo)
                .DefaultIfEmpty(81)
                .Min();

            var recommended = remaining
                .Where(x => x.SemesterNo == firstIncompleteSemester)
                .Where(x => ArePrerequisitesSatisfied(x.Prerequisite, passedCourseIds))
                .Select(x => new RecommendedCourseVm
                {
                    CourseId = x.CourseId,
                    CourseName = x.CourseName ?? x.CourseId,
                    Hours = x.Hours,
                    SemesterNo = x.SemesterNo,
                    DisplayOrder = x.DisplayOrder,
                    Reason = BuildReason(x.Prerequisite)
                })
                .OrderBy(x => x.DisplayOrder)
                .ToList();

            if (!recommended.Any())
            {
                recommended = remaining
                    .Where(x => ArePrerequisitesSatisfied(x.Prerequisite, passedCourseIds))
                    .Select(x => new RecommendedCourseVm
                    {
                        CourseId = x.CourseId,
                        CourseName = x.CourseName ?? x.CourseId,
                        Hours = x.Hours,
                        SemesterNo = x.SemesterNo,
                        DisplayOrder = x.DisplayOrder,
                        Reason = BuildReason(x.Prerequisite)
                    })
                    .OrderBy(x => x.SemesterNo)
                    .ThenBy(x => x.DisplayOrder)
                    .ToList();
            }

            return recommended;
        }

        private bool ArePrerequisitesSatisfied(string? prerequisiteText, HashSet<string> passedCourseIds)
        {
            if (string.IsNullOrWhiteSpace(prerequisiteText))
                return true;

            var prerequisiteCodes = ExtractCourseCodes(prerequisiteText);

            if (!prerequisiteCodes.Any())
                return true;

            return prerequisiteCodes.All(code => passedCourseIds.Contains(NormalizeCourseId(code)));
        }

        private List<string> ExtractCourseCodes(string text)
        {
            var list = new List<string>();

            if (string.IsNullOrWhiteSpace(text))
                return list;

            var matches = Regex.Matches(text, @"\b([A-Z]{2,6})\s*-?\s*(\d{3,4})\b", RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var code = $"{match.Groups[1].Value.ToUpper()}{match.Groups[2].Value}";
                    list.Add(NormalizeCourseId(code));
                }
            }

            return list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private string NormalizeCourseId(string? courseId)
        {
            if (string.IsNullOrWhiteSpace(courseId))
                return string.Empty;

            return courseId
                .Trim()
                .ToUpperInvariant()
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("/", "")
                .Replace("-", "");
        }

        private string BuildReason(string? prerequisiteText)
        {
            if (string.IsNullOrWhiteSpace(prerequisiteText))
                return "Remaining course in the next incomplete semester.";

            return "Remaining course and prerequisites are satisfied.";
        }
    }
}
