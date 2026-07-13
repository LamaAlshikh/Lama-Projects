using Db = Acadify.Models.Db;
using Acadify.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Controllers
{
    [Route("[controller]/[action]")]
    public class GraduationProjectEligibilityController : Controller
    {
        private readonly Db.AcadifyDbContext _context;

        public GraduationProjectEligibilityController(Db.AcadifyDbContext context)
        {
            _context = context;
        }

        // =========================
        // Helpers
        // =========================
        private int? GetCurrentStudentId()
        {
            return HttpContext.Session.GetInt32("StudentId");
        }

        private async Task<int?> GetAdvisorIdForStudentAsync(int studentId)
        {
            // تم الحفاظ على الاستعلام المباشر لضمان جلب الـ Advisor المرتبط حالياً
            return await _context.Students
                .Where(s => s.StudentId == studentId)
                .Select(s => (int?)s.AdvisorId)
                .FirstOrDefaultAsync();
        }

        private async Task<int> GetOrCreateLatestForm5ForStudentAsync(int studentId)
        {
            var latestForm5 = await _context.Forms
                .Where(f => f.StudentId == studentId && f.FormType == "Form 5")
                .OrderByDescending(f => f.FormDate)
                .ThenByDescending(f => f.FormId)
                .FirstOrDefaultAsync();

            if (latestForm5 == null)
            {
                var advisorId = await GetAdvisorIdForStudentAsync(studentId);

                if (!advisorId.HasValue || advisorId.Value <= 0)
                    throw new InvalidOperationException("No advisor is assigned to this student.");

                latestForm5 = new Db.Form
                {
                    StudentId = studentId,
                    AdvisorId = advisorId.Value,
                    FormType = "Form 5",
                    FormDate = DateTime.Now,
                    FormStatus = "Pending",
                    AutoFilled = true
                };

                _context.Forms.Add(latestForm5);
                await _context.SaveChangesAsync();

                var details = new Db.GraduationProjectEligibilityForm
                {
                    FormId = latestForm5.FormId,
                    Eligibility = null,
                    RequiredCoursesStatus = null
                };

                _context.GraduationProjectEligibilityForms.Add(details);
                await _context.SaveChangesAsync();
            }
            else
            {
                var details = await _context.GraduationProjectEligibilityForms
                    .FirstOrDefaultAsync(g => g.FormId == latestForm5.FormId);

                if (details == null)
                {
                    details = new Db.GraduationProjectEligibilityForm
                    {
                        FormId = latestForm5.FormId,
                        Eligibility = null,
                        RequiredCoursesStatus = null
                    };

                    _context.GraduationProjectEligibilityForms.Add(details);
                    await _context.SaveChangesAsync();
                }
            }

            return latestForm5.FormId;
        }

        // Snapshot Logic for storing course statuses in string format
        private static string BuildSnapshot(
            bool cpis351,
            bool cpis358,
            bool cpis323,
            bool cpis380,
            bool cpis357,
            bool cpis342)
        {
            return string.Join(";",
                $"CPIS351={(cpis351 ? 1 : 0)}",
                $"CPIS358={(cpis358 ? 1 : 0)}",
                $"CPIS323={(cpis323 ? 1 : 0)}",
                $"CPIS380={(cpis380 ? 1 : 0)}",
                $"CPIS357={(cpis357 ? 1 : 0)}",
                $"CPIS342={(cpis342 ? 1 : 0)}"
            );
        }

        private static Dictionary<string, string> ParseSnapshot(string? raw)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(raw))
                return map;

            var parts = raw.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var index = part.IndexOf('=');

                if (index <= 0)
                    continue;

                map[part.Substring(0, index).Trim()] = part[(index + 1)..].Trim();
            }

            return map;
        }

        private static bool GetBool(Dictionary<string, string> map, string key)
        {
            if (!map.TryGetValue(key, out var value))
                return false;

            return value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        private static void FillVmFromSnapshot(GraduationProjectEligibilityFormVM vm, string? snapshot)
        {
            var map = ParseSnapshot(snapshot);

            vm.CPIS351 = GetBool(map, "CPIS351");
            vm.CPIS358 = GetBool(map, "CPIS358");
            vm.CPIS323 = GetBool(map, "CPIS323");
            vm.CPIS380 = GetBool(map, "CPIS380");
            vm.CPIS357 = GetBool(map, "CPIS357");
            vm.CPIS342 = GetBool(map, "CPIS342");

            vm.IsEligible =
                vm.CPIS351 &&
                vm.CPIS358 &&
                vm.CPIS323 &&
                vm.CPIS380 &&
                vm.CPIS357 &&
                vm.CPIS342;
        }

        private static string NormalizeCourseCode(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value
                .Trim()
                .ToUpperInvariant()
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("_", "");
        }

        private static string NormalizeText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value.ToUpperInvariant()
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("_", "")
                .Replace("&", "AND")
                .Replace("(", "")
                .Replace(")", "");
        }

        private static HashSet<string> GetForm5CoursesFromTranscript(Db.Transcript? transcript)
        {
            var courses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (transcript == null)
                return courses;

            if (!string.IsNullOrWhiteSpace(transcript.ExtractedCourses))
            {
                var extractedCourses = transcript.ExtractedCourses
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(NormalizeCourseCode)
                    .Where(c => !string.IsNullOrWhiteSpace(c));

                foreach (var course in extractedCourses)
                    courses.Add(course);
            }

            foreach (var course in transcript.Courses)
            {
                var code = NormalizeCourseCode(course.CourseId);

                if (!string.IsNullOrWhiteSpace(code))
                    courses.Add(code);
            }

            var text = NormalizeText(transcript.ExtractedInfo);

            if (!string.IsNullOrWhiteSpace(text))
            {
                if (text.Contains("CPIS351") || text.Contains("ISANALYSISANDARCHITECTUREDES"))
                    courses.Add("CPIS351");

                if (text.Contains("CPIS358") || text.Contains("INTERNETAPPLICATIONSANDWEBPRO"))
                    courses.Add("CPIS358");

                if (text.Contains("CPIS323") || text.Contains("SUMMERWORKPLACETRAINING"))
                    courses.Add("CPIS323");

                if (text.Contains("CPIS380") || text.Contains("INTRODUCTIONTOEBUSINESSSYS"))
                    courses.Add("CPIS380");

                if (text.Contains("CPIS357") || text.Contains("SOFTWAREQUALITYANDTESTING"))
                    courses.Add("CPIS357");

                if (text.Contains("CPIS342") || text.Contains("DATAMININGANDWAREHOUSING") || text.Contains("DATAMINING"))
                    courses.Add("CPIS342");
            }

            return courses;
        }

        private static void MergeVmWithTranscriptCourses(
            GraduationProjectEligibilityFormVM vm,
            HashSet<string> completedCourses)
        {
            vm.CPIS351 = vm.CPIS351 || completedCourses.Contains("CPIS351");
            vm.CPIS358 = vm.CPIS358 || completedCourses.Contains("CPIS358");
            vm.CPIS323 = vm.CPIS323 || completedCourses.Contains("CPIS323");
            vm.CPIS380 = vm.CPIS380 || completedCourses.Contains("CPIS380");
            vm.CPIS357 = vm.CPIS357 || completedCourses.Contains("CPIS357");
            vm.CPIS342 = vm.CPIS342 || completedCourses.Contains("CPIS342");
        }

        private static void SetEligibility(GraduationProjectEligibilityFormVM vm)
        {
            vm.IsEligible =
                vm.CPIS351 &&
                vm.CPIS358 &&
                vm.CPIS323 &&
                vm.CPIS380 &&
                vm.CPIS357 &&
                vm.CPIS342;

            vm.Eligibility = vm.IsEligible ? "Eligible" : "Not Eligible";
        }

        // =========================
        // Actions
        // =========================

        [HttpGet]
        public async Task<IActionResult> Form5(int? formId, bool editMode = false)
        {
            int selectedFormId;
            var sessionStudentId = GetCurrentStudentId();

            if (formId.HasValue && formId.Value > 0)
            {
                selectedFormId = formId.Value;
            }
            else
            {
                if (!sessionStudentId.HasValue)
                    return BadRequest("Session expired.");

                selectedFormId = await GetOrCreateLatestForm5ForStudentAsync(sessionStudentId.Value);
            }

            var form5Entity = await _context.GraduationProjectEligibilityForms
                .Include(x => x.Form)
                    .ThenInclude(f => f.Student)
                .FirstOrDefaultAsync(x => x.FormId == selectedFormId);

            if (form5Entity == null || form5Entity.Form == null)
                return NotFound();

            // حماية أمنية: منع الوصول لنموذج طالب آخر
            if (sessionStudentId.HasValue && form5Entity.Form.StudentId != sessionStudentId.Value)
                return Forbid();

            var transcript = await _context.Transcripts
                .Include(t => t.Courses)
                .FirstOrDefaultAsync(t => t.StudentId == form5Entity.Form.StudentId);

            var vm = new GraduationProjectEligibilityFormVM
            {
                FormId = form5Entity.FormId,
                Form = form5Entity.Form,
                StudentName = form5Entity.Form.Student?.Name ?? "-",
                StudentId = form5Entity.Form.StudentId.ToString(),
                IsEditMode = editMode
            };

            bool transcriptMissing =
                transcript == null ||
                string.IsNullOrWhiteSpace(transcript.PdfFile);

            ViewBag.TranscriptMissing = transcriptMissing;

            if (transcriptMissing)
                return View(vm);

            // تحميل البيانات من السجل المحفوظ إن وجد
            if (!string.IsNullOrWhiteSpace(form5Entity.RequiredCoursesStatus))
            {
                FillVmFromSnapshot(vm, form5Entity.RequiredCoursesStatus);
            }

            // قراءة إضافية خاصة بفورم 5 فقط من الترانسكربت
            var transcriptCourses = GetForm5CoursesFromTranscript(transcript);
            MergeVmWithTranscriptCourses(vm, transcriptCourses);

            SetEligibility(vm);

            // تحديث السجل المحفوظ إذا تغيرت القراءة بعد رفع الترانسكربت
            var newSnapshot = BuildSnapshot(
                vm.CPIS351,
                vm.CPIS358,
                vm.CPIS323,
                vm.CPIS380,
                vm.CPIS357,
                vm.CPIS342);

            if (form5Entity.RequiredCoursesStatus != newSnapshot ||
                form5Entity.Eligibility != vm.Eligibility)
            {
                form5Entity.RequiredCoursesStatus = newSnapshot;
                form5Entity.Eligibility = vm.Eligibility;
                await _context.SaveChangesAsync();
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveUpdate(GraduationProjectEligibilityFormVM vm)
        {
            var entity = await _context.GraduationProjectEligibilityForms
                .Include(x => x.Form)
                .FirstOrDefaultAsync(x => x.FormId == vm.FormId);

            if (entity == null || entity.Form == null)
                return NotFound();

            var sessionStudentId = GetCurrentStudentId();
            if (HttpContext.Session.GetString("UserRole") != "Advisor")
                return RedirectToAction("Login", "Account");

            entity.RequiredCoursesStatus = BuildSnapshot(
                vm.CPIS351,
                vm.CPIS358,
                vm.CPIS323,
                vm.CPIS380,
                vm.CPIS357,
                vm.CPIS342);

            entity.Eligibility =
                (vm.CPIS351 &&
                 vm.CPIS358 &&
                 vm.CPIS323 &&
                 vm.CPIS380 &&
                 vm.CPIS357 &&
                 vm.CPIS342)
                    ? "Eligible"
                    : "Not Eligible";

            entity.Form.FormStatus = "Updated";
            entity.Form.FormDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["ActionMessage"] = "The form is updated successfully.";
            return RedirectToAction("Form5", new { formId = vm.FormId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int formId, string status)
        {
            var form = await _context.Forms.FindAsync(formId);

            if (form == null)
                return NotFound();

            form.FormStatus = status;
            form.FormDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["ActionMessage"] = $"The form status is updated to {status}.";
            return RedirectToAction("Form5", new { formId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendToAdvisingCommittee(int formId)
        {
            var form = await _context.Forms.FindAsync(formId);

            if (form == null)
                return NotFound();

            form.FormStatus = "Sent to Advising Committee";
            form.FormDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["ActionMessage"] = "The form is sent to the Advising Committee successfully.";
            return RedirectToAction("Form5", new { formId });
        }
    }
}