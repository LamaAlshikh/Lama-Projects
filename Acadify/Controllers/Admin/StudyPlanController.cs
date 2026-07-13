using Acadify.Models.AdminPages;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Db = Acadify.Models.Db;

namespace Acadify.Controllers.Admin
{
    [Route("Admin")]
    public class StudyPlanController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly Db.AcadifyDbContext _db;

        public StudyPlanController(IWebHostEnvironment env, Db.AcadifyDbContext db)
        {
            _env = env;
            _db = db;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("UserRole") == "Admin";
        }

        [HttpGet("UploadStudyPlan")]
        public IActionResult UploadStudyPlan()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            return View("~/Views/Admin/UploadStudyPlan.cshtml", new UploadStudyPlanModel());
        }

        [HttpPost("UploadStudyPlan")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadStudyPlan(UploadStudyPlanModel model)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                model.Message = "Please upload Study Plan Excel file.";
                return View("~/Views/Admin/UploadStudyPlan.cshtml", model);
            }

            if (model.StudyPlanFile == null || model.StudyPlanFile.Length == 0)
            {
                ModelState.AddModelError("StudyPlanFile", "Please upload Study Plan Excel file.");
                model.Message = "Please upload Study Plan Excel file.";
                return View("~/Views/Admin/UploadStudyPlan.cshtml", model);
            }

            var ext = Path.GetExtension(model.StudyPlanFile.FileName).ToLower();

            if (ext != ".xlsx")
            {
                ModelState.AddModelError("StudyPlanFile", "Only Excel .xlsx files are allowed.");
                model.Message = "Only Excel .xlsx files are allowed.";
                return View("~/Views/Admin/UploadStudyPlan.cshtml", model);
            }

            var folder = Path.Combine(_env.WebRootPath, "uploads", "study-plan");
            Directory.CreateDirectory(folder);

            var savedFileName = $"StudyPlan_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            var savedPath = Path.Combine(folder, savedFileName);

            using (var stream = new FileStream(savedPath, FileMode.Create))
            {
                await model.StudyPlanFile.CopyToAsync(stream);
            }

            List<ExtractedStudyPlanCourse> extractedCourses;

            try
            {
                extractedCourses = ExtractStudyPlanCoursesFromExcel(savedPath);
            }
            catch
            {
                model.SavedFileName = savedFileName;
                model.Message = "The Excel file was uploaded, but it could not be read. Please use the provided StudyPlan Excel template.";
                return View("~/Views/Admin/UploadStudyPlan.cshtml", model);
            }

            if (extractedCourses.Count == 0)
            {
                model.SavedFileName = savedFileName;
                model.Message = "The Excel file was uploaded, but no courses were extracted. Please make sure the file contains the required columns.";
                return View("~/Views/Admin/UploadStudyPlan.cshtml", model);
            }

            var totalHours = extractedCourses.Sum(c => c.Hours);

            var studyPlan = await _db.StudyPlans
                .OrderByDescending(s => s.PlanId)
                .FirstOrDefaultAsync();

            if (studyPlan == null)
            {
                studyPlan = new Db.StudyPlan
                {
                    Major = "Information Systems",
                    TotalHours = totalHours,

                    // We keep using PdfFile because this is the existing database column name.
                    // It will now store the uploaded Excel file name.
                    PdfFile = savedFileName
                };

                _db.StudyPlans.Add(studyPlan);
                await _db.SaveChangesAsync();
            }
            else
            {
                studyPlan.Major = "Information Systems";
                studyPlan.TotalHours = totalHours;
                studyPlan.PdfFile = savedFileName;

                await _db.SaveChangesAsync();
            }

            await SaveExtractedStudyPlanCoursesAsync(studyPlan.PlanId, extractedCourses);

            model.SavedFileName = savedFileName;
            model.Message = $"Study plan uploaded successfully. {extractedCourses.Count} courses were extracted and saved.";

            return View("~/Views/Admin/UploadStudyPlan.cshtml", model);
        }
        private async Task SaveExtractedStudyPlanCoursesAsync(int planId, List<ExtractedStudyPlanCourse> extractedCourses)
        {
            var oldPlanCourses = await _db.StudyPlanCourses
                .Where(x => x.PlanId == planId)
                .ToListAsync();

            _db.StudyPlanCourses.RemoveRange(oldPlanCourses);
            await _db.SaveChangesAsync();

            var cleanCourses = extractedCourses
                .Where(c => !string.IsNullOrWhiteSpace(c.CourseId))
                .GroupBy(c => c.CourseId)
                .Select(g => g.First())
                .OrderBy(c => c.SemesterNo)
                .ThenBy(c => c.DisplayOrder)
                .ToList();

            foreach (var item in cleanCourses)
            {
                item.CourseId = LimitText(NormalizeCourseId(item.CourseId), 30) ?? "";
                item.CourseName = LimitText(item.CourseName, 50) ?? item.CourseId;
                item.Prerequisite = LimitText(item.Prerequisite, 100);
                item.RequirementCategory = LimitText(item.RequirementCategory, 50);

                var existingCourse = await _db.Courses
                    .FirstOrDefaultAsync(c => c.CourseId == item.CourseId);

                if (existingCourse == null)
                {
                    existingCourse = new Db.Course
                    {
                        CourseId = item.CourseId,
                        CourseName = item.CourseName,
                        Hours = item.Hours,
                        Prerequisite = item.Prerequisite,
                        RequirementCategory = item.RequirementCategory
                    };

                    _db.Courses.Add(existingCourse);
                }
                else
                {
                    existingCourse.CourseName = item.CourseName;
                    existingCourse.Hours = item.Hours;
                    existingCourse.Prerequisite = item.Prerequisite;
                    existingCourse.RequirementCategory = item.RequirementCategory;
                }
            }

            await _db.SaveChangesAsync();

            foreach (var item in cleanCourses)
            {
                var planCourse = new Db.StudyPlanCourse
                {
                    PlanId = planId,
                    CourseId = item.CourseId,
                    SemesterNo = item.SemesterNo,
                    DisplayOrder = item.DisplayOrder
                };

                _db.StudyPlanCourses.Add(planCourse);
            }

            await _db.SaveChangesAsync();
        }
        private static bool IsFreeOrElectiveCourse(ExtractedStudyPlanCourse course)
        {
            var courseId = course.CourseId ?? "";
            var courseName = course.CourseName ?? "";
            var category = course.RequirementCategory ?? "";

            return courseId.Contains("FREE", StringComparison.OrdinalIgnoreCase) ||
                   courseId.Contains("ELEC", StringComparison.OrdinalIgnoreCase) ||
                   courseName.Contains("Free", StringComparison.OrdinalIgnoreCase) ||
                   courseName.Contains("Elective", StringComparison.OrdinalIgnoreCase) ||
                   category.Contains("Free", StringComparison.OrdinalIgnoreCase) ||
                   category.Contains("Elective", StringComparison.OrdinalIgnoreCase);
        }
        private static List<ExtractedStudyPlanCourse> ExtractStudyPlanCoursesFromExcel(string excelPath)
        {
            var courses = new List<ExtractedStudyPlanCourse>();

            using var workbook = new XLWorkbook(excelPath);

            var worksheet = workbook.Worksheets
                .FirstOrDefault(w => w.Name.Equals("StudyPlan", StringComparison.OrdinalIgnoreCase))
                ?? workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
                return courses;

            var usedRange = worksheet.RangeUsed();

            if (usedRange == null)
                return courses;

            var headerRowNumber = FindHeaderRow(worksheet);

            if (headerRowNumber == 0)
                return courses;

            var columnMap = GetColumnMap(worksheet, headerRowNumber);

            if (!columnMap.ContainsKey("CourseId") ||
                !columnMap.ContainsKey("CourseName") ||
                !columnMap.ContainsKey("Hours") ||
                !columnMap.ContainsKey("SemesterNo"))
            {
                return courses;
            }

            var lastRow = usedRange.LastRow().RowNumber();

            var semesterDisplayOrder = new Dictionary<int, int>();

            int collegeFreeCounter = 1;
            int departmentElectiveCounter = 1;

            for (int rowNumber = headerRowNumber + 1; rowNumber <= lastRow; rowNumber++)
            {
                var row = worksheet.Row(rowNumber);

                var courseId = NormalizeCourseId(GetCellText(row, columnMap, "CourseId"));
                var courseName = GetCellText(row, columnMap, "CourseName");
                var hoursText = GetCellText(row, columnMap, "Hours");
                var prerequisite = NormalizeNullableText(GetCellText(row, columnMap, "Prerequisite"));
                var semesterText = GetCellText(row, columnMap, "SemesterNo");
                var displayOrderText = GetCellText(row, columnMap, "DisplayOrder");
                var requirementCategory = NormalizeNullableText(GetCellText(row, columnMap, "RequirementCategory"));

                if (string.IsNullOrWhiteSpace(courseName))
                    courseName = courseId;

                if (string.IsNullOrWhiteSpace(requirementCategory))
                    requirementCategory = InferRequirementCategory(courseId, courseName);

                bool isCollegeFree =
                    courseName.Contains("Free", StringComparison.OrdinalIgnoreCase) ||
                    (requirementCategory ?? "").Contains("College Free", StringComparison.OrdinalIgnoreCase);

                bool isDepartmentElective =
                    courseName.Contains("Elective", StringComparison.OrdinalIgnoreCase) ||
                    (requirementCategory ?? "").Contains("Department Elective", StringComparison.OrdinalIgnoreCase);

                if (string.IsNullOrWhiteSpace(courseId) ||
                    courseId == "-" ||
                    courseId == "---")
                {
                    if (isCollegeFree)
                    {
                        courseId = $"COL-FREE-{collegeFreeCounter}";
                        collegeFreeCounter++;
                    }
                    else if (isDepartmentElective)
                    {
                        courseId = $"DEPT-ELEC-{departmentElectiveCounter}";
                        departmentElectiveCounter++;
                    }
                    else
                    {
                        continue;
                    }
                }

                if (isCollegeFree)
                    requirementCategory = "College Free";

                if (isDepartmentElective)
                    requirementCategory = "Department Elective";

                var hours = ParseInt(hoursText, 3);
                var semesterNo = ParseInt(semesterText, 0);

                if (semesterNo == 0)
                    continue;

                if (!semesterDisplayOrder.ContainsKey(semesterNo))
                    semesterDisplayOrder[semesterNo] = 1;

                var displayOrder = ParseInt(displayOrderText, semesterDisplayOrder[semesterNo]);

                courses.Add(new ExtractedStudyPlanCourse
                {
                    CourseId = LimitText(courseId, 30) ?? courseId,
                    CourseName = LimitText(courseName, 50) ?? courseId,
                    Hours = hours,
                    Prerequisite = LimitText(prerequisite, 100),
                    SemesterNo = semesterNo,
                    DisplayOrder = displayOrder,
                    RequirementCategory = LimitText(requirementCategory, 50)
                });

                semesterDisplayOrder[semesterNo]++;
            }

            return courses
                .Where(c => !string.IsNullOrWhiteSpace(c.CourseId))
                .GroupBy(c => c.CourseId)
                .Select(g => g.First())
                .OrderBy(c => c.SemesterNo)
                .ThenBy(c => c.DisplayOrder)
                .ToList();
        }
        private static int FindHeaderRow(IXLWorksheet worksheet)
        {
            var usedRange = worksheet.RangeUsed();

            if (usedRange == null)
                return 0;

            var firstRow = usedRange.FirstRow().RowNumber();
            var lastRowToCheck = Math.Min(usedRange.LastRow().RowNumber(), firstRow + 20);

            for (int rowNumber = firstRow; rowNumber <= lastRowToCheck; rowNumber++)
            {
                var row = worksheet.Row(rowNumber);

                var headers = row.CellsUsed()
                    .Select(c => NormalizeHeader(c.GetString()))
                    .Where(h => !string.IsNullOrWhiteSpace(h))
                    .ToList();

                var hasCourseId = headers.Any(IsCourseIdHeader);
                var hasCourseName = headers.Any(IsCourseNameHeader);
                var hasHours = headers.Any(IsHoursHeader);
                var hasSemester = headers.Any(IsSemesterHeader);

                if (hasCourseId && hasCourseName && hasHours && hasSemester)
                    return rowNumber;
            }

            return 0;
        }

        private static Dictionary<string, int> GetColumnMap(IXLWorksheet worksheet, int headerRowNumber)
        {
            var map = new Dictionary<string, int>();

            var headerRow = worksheet.Row(headerRowNumber);

            foreach (var cell in headerRow.CellsUsed())
            {
                var header = NormalizeHeader(cell.GetString());
                var columnNumber = cell.Address.ColumnNumber;

                if (IsCourseIdHeader(header))
                    map["CourseId"] = columnNumber;
                else if (IsCourseNameHeader(header))
                    map["CourseName"] = columnNumber;
                else if (IsHoursHeader(header))
                    map["Hours"] = columnNumber;
                else if (IsPrerequisiteHeader(header))
                    map["Prerequisite"] = columnNumber;
                else if (IsSemesterHeader(header))
                    map["SemesterNo"] = columnNumber;
                else if (IsDisplayOrderHeader(header))
                    map["DisplayOrder"] = columnNumber;
                else if (IsRequirementCategoryHeader(header))
                    map["RequirementCategory"] = columnNumber;
            }

            return map;
        }

        private static string GetCellText(IXLRow row, Dictionary<string, int> columnMap, string key)
        {
            if (!columnMap.ContainsKey(key))
                return "";

            var value = row.Cell(columnMap[key]).GetString();

            return value?.Trim() ?? "";
        }

        private static string NormalizeHeader(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            value = value.Trim().ToLower();
            value = Regex.Replace(value, @"[\s_\-]+", "");

            return value;
        }

        private static bool IsCourseIdHeader(string header)
        {
            return header == "courseid" ||
                   header == "coursecode" ||
                   header == "code";
        }

        private static bool IsCourseNameHeader(string header)
        {
            return header == "coursename" ||
                   header == "course" ||
                   header == "title" ||
                   header == "name";
        }

        private static bool IsHoursHeader(string header)
        {
            return header == "hours" ||
                   header == "credithours" ||
                   header == "cr" ||
                   header == "credits";
        }

        private static bool IsPrerequisiteHeader(string header)
        {
            return header == "prerequisite" ||
                   header == "prerequisites" ||
                   header == "pre" ||
                   header == "prereq";
        }

        private static bool IsSemesterHeader(string header)
        {
            return header == "semesterno" ||
                   header == "semester" ||
                   header == "level" ||
                   header == "term";
        }

        private static bool IsDisplayOrderHeader(string header)
        {
            return header == "displayorder" ||
                   header == "order" ||
                   header == "sequence" ||
                   header == "sort";
        }

        private static bool IsRequirementCategoryHeader(string header)
        {
            return header == "requirementcategory" ||
                   header == "category" ||
                   header == "graduationrequirement" ||
                   header == "requirement";
        }

        private static int ParseInt(string value, int defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            value = ToEnglishDigits(value).Trim();

            if (int.TryParse(value, out int number))
                return number;

            var match = Regex.Match(value, @"-?\d+");

            if (match.Success && int.TryParse(match.Value, out number))
                return number;

            return defaultValue;
        }

        private static string? NormalizeNullableText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            value = value.Trim();

            if (value == "-" || value == "---" || value.Equals("NULL", StringComparison.OrdinalIgnoreCase))
                return null;

            return value;
        }

        private static string? InferRequirementCategory(string courseId, string courseName)
        {
            if (string.IsNullOrWhiteSpace(courseId))
                return null;

            courseId = NormalizeCourseId(courseId);

            if (courseId.StartsWith("ISLS-", StringComparison.OrdinalIgnoreCase) ||
                courseId.StartsWith("ARAB-", StringComparison.OrdinalIgnoreCase))
            {
                return "University Requirement";
            }

            if (courseId.StartsWith("COL-FREE", StringComparison.OrdinalIgnoreCase))
                return "College Free";

            if (courseId.StartsWith("DEPT-ELEC", StringComparison.OrdinalIgnoreCase))
                return "Department Elective";

            if (courseId.StartsWith("CPIT-", StringComparison.OrdinalIgnoreCase) ||
                courseId.StartsWith("CPCS-", StringComparison.OrdinalIgnoreCase) ||
                courseId.StartsWith("STAT-", StringComparison.OrdinalIgnoreCase))
            {
                return "College Mandatory";
            }

            if (courseId.StartsWith("BUS-", StringComparison.OrdinalIgnoreCase) ||
                courseId.StartsWith("MRKT-", StringComparison.OrdinalIgnoreCase) ||
                courseId.StartsWith("ACCT-", StringComparison.OrdinalIgnoreCase))
            {
                return "College Requirement";
            }

            if (courseId.StartsWith("CPIS-", StringComparison.OrdinalIgnoreCase))
                return "Department Mandatory";

            if (!string.IsNullOrWhiteSpace(courseName) &&
                courseName.Contains("Elective", StringComparison.OrdinalIgnoreCase))
            {
                return "Department Elective";
            }

            if (!string.IsNullOrWhiteSpace(courseName) &&
                courseName.Contains("Free", StringComparison.OrdinalIgnoreCase))
            {
                return "College Free";
            }

            return null;
        }

        private static string NormalizeCourseId(string courseId)
        {
            if (string.IsNullOrWhiteSpace(courseId))
                return "";

            courseId = ToEnglishDigits(courseId);
            courseId = courseId.Trim().ToUpper();
            courseId = Regex.Replace(courseId, @"\s+", "");
            courseId = courseId.Replace("–", "-").Replace("—", "-");

            return courseId;
        }

        private static string ToEnglishDigits(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return input
                .Replace('٠', '0')
                .Replace('١', '1')
                .Replace('٢', '2')
                .Replace('٣', '3')
                .Replace('٤', '4')
                .Replace('٥', '5')
                .Replace('٦', '6')
                .Replace('٧', '7')
                .Replace('٨', '8')
                .Replace('٩', '9')
                .Replace('۰', '0')
                .Replace('۱', '1')
                .Replace('۲', '2')
                .Replace('۳', '3')
                .Replace('۴', '4')
                .Replace('۵', '5')
                .Replace('۶', '6')
                .Replace('۷', '7')
                .Replace('۸', '8')
                .Replace('۹', '9');
        }

        private static string? LimitText(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            value = Regex.Replace(value.Trim(), @"\s+", " ");

            if (value.Length <= maxLength)
                return value;

            return value.Substring(0, maxLength).Trim();
        }

        private sealed class ExtractedStudyPlanCourse
        {
            public string CourseId { get; set; } = "";
            public string CourseName { get; set; } = "";
            public int Hours { get; set; }
            public string? Prerequisite { get; set; }
            public int SemesterNo { get; set; }
            public int DisplayOrder { get; set; }
            public string? RequirementCategory { get; set; }
        }
    }
}