using Acadify.Models;
using Db = Acadify.Models.Db;
using Acadify.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UglyToad.PdfPig;

namespace Acadify.Controllers
{
    public class AdvisorController : Controller
    {
        private readonly Db.AcadifyDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly AiAcademicAgentService _aiSummaryService;

        private const string Form1SessionKey = "Form1Draft";
        private const string Form4SessionKey = "Form4Draft";
        private const string SelectedStudentSessionKey = "SelectedStudentId";

        public AdvisorController(
            Db.AcadifyDbContext context,
            IWebHostEnvironment env,
            AiAcademicAgentService aiSummaryService)
        {
            _context = context;
            _env = env;
            _aiSummaryService = aiSummaryService;
        }

        /* ========================================================
                            Shared Helpers
           ======================================================== */
        private static List<AdvisorHomeTranscriptTermBlock> ReadAdvisorHomeCompressedTranscriptTermsForMatching(
    string? extractedInfo)
        {
            var result = new List<AdvisorHomeTranscriptTermBlock>();

            if (string.IsNullOrWhiteSpace(extractedInfo))
                return result;

            var text = Regex.Replace(extractedInfo, @"\s+", " ").Trim();

            var termMatches = Regex.Matches(
                text,
                @"\b(FALL|SPRING|SUMMER|WINTER)\s+\d{4}/\d{4}",
                RegexOptions.IgnoreCase);

            if (termMatches.Count == 0)
                return result;

            for (int i = 0; i < termMatches.Count; i++)
            {
                var currentMatch = termMatches[i];

                int startIndex = currentMatch.Index;
                int endIndex = (i + 1 < termMatches.Count)
                    ? termMatches[i + 1].Index
                    : text.Length;

                if (endIndex <= startIndex)
                    continue;

                var termText = text.Substring(startIndex, endIndex - startIndex);

                var termBlock = new AdvisorHomeTranscriptTermBlock
                {
                    TermName = currentMatch.Value.ToUpperInvariant()
                };

                var courseMatches = Regex.Matches(
                    termText,
                    @"\b([A-Z]{2,6})\s*-?\s*(\d{3,4})\b",
                    RegexOptions.IgnoreCase);

                foreach (Match courseMatch in courseMatches)
                {
                    var courseId = NormalizeCourseIdForAdvisorHomeMatching(
                        courseMatch.Groups[1].Value + courseMatch.Groups[2].Value
                    );

                    if (!string.IsNullOrWhiteSpace(courseId))
                        termBlock.CourseIds.Add(courseId);
                }

                if (termBlock.CourseIds.Any())
                    result.Add(termBlock);
            }

            return result;
        }
        private static string GetAdvisorHomeStudyPlanMatchStatus(
    Db.Student student,
    List<Db.StudyPlanCourse> planCourses)
        {
            var planLevels = BuildAdvisorHomePlanLevelsForMatching(planCourses);

            if (!planLevels.Any())
                return "not matched";

            var transcriptTerms = ReadAdvisorHomeCompressedTranscriptTermsForMatching(student.Transcript?.ExtractedInfo);
            if (!transcriptTerms.Any())
                return "not matched";

            var allPlanCourseIds = planLevels
                .SelectMany(level => level.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var transcriptPlanTerms = transcriptTerms
                .Select(term => new
                {
                    term.TermName,
                    CourseIds = term.CourseIds
                        .Where(courseId => allPlanCourseIds.Contains(courseId))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase)
                })
                .Where(term => term.CourseIds.Any())
                .ToList();

            if (!transcriptPlanTerms.Any())
                return "not matched";

            var orderedPlanLevels = planLevels
                .OrderBy(level => level.Key)
                .ToList();

            if (transcriptPlanTerms.Count > orderedPlanLevels.Count)
                return "not matched";

            for (int i = 0; i < transcriptPlanTerms.Count; i++)
            {
                var expectedCourses = orderedPlanLevels[i].Value;
                var actualCourses = transcriptPlanTerms[i].CourseIds;

                if (!expectedCourses.SetEquals(actualCourses))
                    return "not matched";
            }

            return "matched";
        }

        private sealed class AdvisorHomeTranscriptTermBlock
        {
            public string TermName { get; set; } = "";
            public HashSet<string> CourseIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private static Dictionary<int, HashSet<string>> BuildAdvisorHomePlanLevelsForMatching(
            List<Db.StudyPlanCourse> planCourses)
        {
            var result = new Dictionary<int, HashSet<string>>();

            foreach (var item in planCourses)
            {
                var courseId = NormalizeCourseIdForAdvisorHomeMatching(item.CourseId);

                if (IsIgnoredAdvisorHomePlanCourse(courseId))
                    continue;

                int semesterNo;

                try
                {
                    semesterNo = Convert.ToInt32(item.SemesterNo);
                }
                catch
                {
                    continue;
                }

                if (semesterNo <= 0)
                    continue;

                if (!result.ContainsKey(semesterNo))
                    result[semesterNo] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                result[semesterNo].Add(courseId);
            }

            return result;
        }

        private static List<AdvisorHomeTranscriptTermBlock> ReadAdvisorHomeTranscriptTermsForMatching(
            string? extractedInfo)
        {
            var result = new List<AdvisorHomeTranscriptTermBlock>();

            if (string.IsNullOrWhiteSpace(extractedInfo))
                return result;

            var lines = extractedInfo
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => Regex.Replace(line, @"\s+", " ").Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            AdvisorHomeTranscriptTermBlock? currentTerm = null;

            foreach (var line in lines)
            {
                var termMatch = Regex.Match(
                    line,
                    @"\b(FALL|SPRING|SUMMER|WINTER)\s+\d{4}/\d{4}",
                    RegexOptions.IgnoreCase);

                if (termMatch.Success)
                {
                    currentTerm = new AdvisorHomeTranscriptTermBlock
                    {
                        TermName = termMatch.Value.ToUpperInvariant()
                    };

                    result.Add(currentTerm);
                    continue;
                }

                if (currentTerm == null)
                    continue;

                var courseMatches = Regex.Matches(
                    line,
                    @"\b([A-Z]{2,6})\s*-?\s*(\d{3,4})\b",
                    RegexOptions.IgnoreCase);

                foreach (Match match in courseMatches)
                {
                    var courseId = NormalizeCourseIdForAdvisorHomeMatching(
                        match.Groups[1].Value + match.Groups[2].Value
                    );

                    if (!string.IsNullOrWhiteSpace(courseId))
                        currentTerm.CourseIds.Add(courseId);
                }
            }

            return result
                .Where(term => term.CourseIds.Any())
                .ToList();
        }

        private static bool IsIgnoredAdvisorHomePlanCourse(string courseId)
        {
            if (string.IsNullOrWhiteSpace(courseId))
                return true;

            return courseId == "---" ||
                   courseId.Contains("FREE", StringComparison.OrdinalIgnoreCase) ||
                   courseId.Contains("ELEC", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeCourseIdForAdvisorHomeMatching(string? courseId)
        {
            if (string.IsNullOrWhiteSpace(courseId))
                return "";

            return courseId
                .Replace("-", "")
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("/", "")
                .Trim()
                .ToUpper();
        }
        private static int GetCohortFromStudentId(int studentId)
{
    var studentIdText = studentId.ToString();

    if (studentIdText.Length < 2)
        return 0;

    var cohortText = studentIdText.Substring(0, 2);

    if (int.TryParse(cohortText, out int cohort))
        return cohort;

    return 0;
}
        private int? GetCurrentAdvisorId()
        {
            return HttpContext.Session.GetInt32("AdvisorId");
        }

        private int? GetSelectedStudentId()
        {
            return HttpContext.Session.GetInt32(SelectedStudentSessionKey);
        }

        private void SetSelectedStudentId(int studentId)
        {
            HttpContext.Session.SetInt32(SelectedStudentSessionKey, studentId);
        }

        private int ResolveStudentId(int? studentId = null)
        {
            if (studentId.HasValue && studentId.Value > 0)
            {
                SetSelectedStudentId(studentId.Value);
                return studentId.Value;
            }

            var selectedStudentId = GetSelectedStudentId();
            if (selectedStudentId.HasValue && selectedStudentId.Value > 0)
                return selectedStudentId.Value;

            // Temporary fallback for prototype pages that are opened directly.
            return 2210783;
        }

        private int ResolveAdvisorId()
        {
            return GetCurrentAdvisorId() ?? 1;
        }
        private static string GetFormTypeFromNumber(int formId)
        {
            return formId switch
            {
                1 => "Form 1",
                2 => "Form 2",
                3 => "Form 3",
                4 => "Form 4",
                5 => "Form 5",
                _ => ""
            };
        }

        private static string GetSentStatusFromNumber(int formId)
        {
            return formId == 2
                ? "Sent to Advising Committee"
                : "Sent";
        }

        private async Task<Db.Form> GetOrCreateLatestBaseFormAsync(int studentId, int advisorId, string formType)
        {
            var form = await _context.Forms
                .Where(f =>
                    f.StudentId == studentId &&
                    f.AdvisorId == advisorId &&
                    f.FormType == formType)
                .OrderByDescending(f => f.FormDate)
                .ThenByDescending(f => f.FormId)
                .FirstOrDefaultAsync();

            if (form == null)
            {
                form = new Db.Form
                {
                    StudentId = studentId,
                    AdvisorId = advisorId,
                    FormType = formType,
                    FormDate = DateTime.Now,
                    FormStatus = "Draft",
                    AdvisorNotes = null,
                    AutoFilled = true,
                    AdvisorConfirmation = false
                };

                _context.Forms.Add(form);
                await _context.SaveChangesAsync();
            }

            return form;
        }

        private static string GetMatchStatusText(object? matchingStatus)
        {
            if (matchingStatus == null)
                return "not matched";

            var type = matchingStatus.GetType();

            var propNames = new[]
            {
                "Status",
                "MatchStatus",
                "MatchingStatus",
                "Result"
            };

            foreach (var propName in propNames)
            {
                var prop = type.GetProperty(propName);
                if (prop != null)
                {
                    var value = prop.GetValue(matchingStatus)?.ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }

            return "matched";
        }
        private static string GetAcademicStatusText(Db.GraduationStatus? graduationStatus)
        {
            if (graduationStatus == null)
                return "Has Remaining Courses";

            if (graduationStatus.RemainingHours <= 0)
                return "Graduated";

            if (graduationStatus.RemainingHours <= 11)
                return "near graduation";

            return "Has Remaining Courses";
        }

        private async Task<Db.Student?> GetAdvisorStudentAsync(int advisorId, int studentId)
        {
            return await _context.Students
                .Include(s => s.GraduationStatus)
                .Include(s => s.MatchingStatus)
                .FirstOrDefaultAsync(s => s.StudentId == studentId && s.AdvisorId == advisorId);
        }

        private async Task<int> GetOrCreateLatestForm5ForStudentAsync(int studentId, int advisorId)
        {
            var latestForm5 = await _context.Forms
                .Where(f => f.StudentId == studentId && f.FormType == "Form 5")
                .OrderByDescending(f => f.FormDate)
                .ThenByDescending(f => f.FormId)
                .FirstOrDefaultAsync();

            if (latestForm5 == null)
            {
                latestForm5 = new Db.Form
                {
                    StudentId = studentId,
                    AdvisorId = advisorId,
                    FormType = "Form 5",
                    FormDate = DateTime.Now,
                    FormStatus = "Pending",
                    AdvisorNotes = null,
                    AutoFilled = true,
                    AdvisorConfirmation = null
                };

                _context.Forms.Add(latestForm5);
                await _context.SaveChangesAsync();
            }

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

            return latestForm5.FormId;
        }

        /* ========================================================
                            Advisor Home
           ======================================================== */

        [HttpGet]
        public async Task<IActionResult> AdvisorHome(string? cohort = null)
        {
            if (HttpContext.Session.GetString("UserRole") != "Advisor")
                return RedirectToAction("Login", "Account");

            int? advisorId = GetCurrentAdvisorId();
            if (!advisorId.HasValue)
                return RedirectToAction("Login", "Account");

            // نجيب آخر خطة دراسية مرفوعة
            var latestPlanId = await _context.StudyPlans
                .OrderByDescending(p => p.PlanId)
                .Select(p => p.PlanId)
                .FirstOrDefaultAsync();

            var planCourses = latestPlanId > 0
                ? await _context.StudyPlanCourses
                    .Where(p => p.PlanId == latestPlanId)
                    .ToListAsync()
                : new List<Db.StudyPlanCourse>();

            // نجيب طالبات المرشد مع الترانسكربت والمواد
            var studentsFromDb = await _context.Students
                .Include(s => s.GraduationStatus)
                .Include(s => s.Transcript)
                    .ThenInclude(t => t.Courses)
                .Where(s => s.AdvisorId == advisorId.Value)
                .OrderBy(s => s.Name)
                .ToListAsync();

            var students = studentsFromDb.Select(s => new AdvisorHomeStudentVM
            {
                StudentId = s.StudentId,
                StudentName = s.Name,

                // الدفعة من أول رقمين في StudentId
                CohortYear = GetCohortFromStudentId(s.StudentId),

                AcademicStatus = GetAcademicStatusText(s.GraduationStatus),

                // الحالة من مقارنة الخطة مع الترانسكربت، مو من الداتابيس
                MatchStatus = GetAdvisorHomeStudyPlanMatchStatus(s, planCourses),
                ImagePath = "~/images/user.png"
            }).ToList();

            // قائمة الدفعات للفلتر
            var cohorts = students
                .Select(s => s.CohortYear)
                .Where(c => c > 0)
                .Distinct()
                .OrderByDescending(c => c)
                .ToList();

            ViewBag.Cohorts = cohorts;

            // تطبيق فلتر الدفعة
            if (!string.IsNullOrWhiteSpace(cohort) && int.TryParse(cohort, out int selectedCohort))
            {
                students = students
                    .Where(s => s.CohortYear == selectedCohort)
                    .ToList();
            }

            students = students
                .OrderByDescending(s => s.CohortYear)
                .ThenBy(s => s.StudentName)
                .ToList();

            var studentIds = students.Select(s => s.StudentId).ToList();

            var meetingMessages = await (
                from meeting in _context.Meetings
                join message in _context.MeetingMessages
                    on meeting.MeetingId equals message.MeetingId
                where meeting.AdvisorId == advisorId.Value
                      && studentIds.Contains(meeting.StudentId)
                      && message.MessageDate != null
                select new
                {
                    meeting.StudentId,
                    message.SenderName,
                    message.MessageDate
                }
            ).ToListAsync();

            var chatAlerts = new Dictionary<int, bool>();

            foreach (var student in students)
            {
                var latestMessage = meetingMessages
                    .Where(m => m.StudentId == student.StudentId)
                    .OrderByDescending(m => m.MessageDate)
                    .FirstOrDefault();

                var cleanSenderName = (latestMessage?.SenderName ?? "")
                    .Replace("(me)", "", StringComparison.OrdinalIgnoreCase)
                    .Trim();

                var studentName = (student.StudentName ?? "").Trim();

                bool hasNewMessage =
                    latestMessage != null &&
                    cleanSenderName.Equals(studentName, StringComparison.OrdinalIgnoreCase);

                chatAlerts[student.StudentId] = hasNewMessage;
            }

            ViewBag.ChatAlerts = chatAlerts;

            return View(students);
        }

        /* ========================================================
                            Student Forms
           ======================================================== */

        [HttpGet]
        public async Task<IActionResult> StudentForms(int? studentId)
        {
            if (HttpContext.Session.GetString("UserRole") != "Advisor")
                return RedirectToAction("Login", "Account");

            int? advisorId = GetCurrentAdvisorId();
            if (!advisorId.HasValue)
                return RedirectToAction("Login", "Account");

            int resolvedStudentId = ResolveStudentId(studentId);

            var student = await GetAdvisorStudentAsync(advisorId.Value, resolvedStudentId);
            if (student == null)
                return NotFound("Student was not found for this advisor.");

            ViewBag.StudentId = student.StudentId;
            ViewBag.StudentName = student.Name;

            var forms = new List<StudentFormsVM>
            {
                new StudentFormsVM
                {
                    FormId = 1,
                    FormTitle = "Academic Advising Confirmation",
                    FormType = "Form1",
                    CanSend = true
                },
                new StudentFormsVM
                {
                    FormId = 2,
                    FormTitle = "Next Semester Course Selection",
                    FormType = "Form2",
                    CanSend = true
                },
                new StudentFormsVM
                {
                    FormId = 3,
                    FormTitle = "Meeting Record Form",
                    FormType = "Form3",
                    CanSend = true
                },
                new StudentFormsVM
                {
                    FormId = 4,
                    FormTitle = "Study Plan Matching",
                    FormType = "Form4",
                    CanSend = true
                },
                new StudentFormsVM
                {
                    FormId = 5,
                    FormTitle = "Graduation Project Eligibility",
                    FormType = "Form5",
                    CanSend = true
                }
            };

            return View(forms);
        }

        public IActionResult TestDb()
        {
            var count = _context.Students.Count();
            return Content("عدد الطلاب: " + count);
        }

        [HttpGet]
        public async Task<IActionResult> ViewForm(int? studentId, int formId)
        {
            int resolvedStudentId = ResolveStudentId(studentId);
            int advisorId = ResolveAdvisorId();

            switch (formId)
            {
                case 1:
                    return RedirectToAction(nameof(Form1), new { studentId = resolvedStudentId });

                case 2:
                    return RedirectToAction(nameof(Form2), new { studentId = resolvedStudentId });

                case 3:
                    return RedirectToAction(nameof(Form3), new { studentId = resolvedStudentId });

                case 4:
                    return RedirectToAction(nameof(Form4), new { studentId = resolvedStudentId });

                case 5:
                    int form5Id = await GetOrCreateLatestForm5ForStudentAsync(resolvedStudentId, advisorId);
                    return RedirectToAction("Form5", "GraduationProjectEligibility", new { formId = form5Id });

                default:
                    return NotFound();
            }
        }

        [HttpGet]
        public async Task<IActionResult> PrintForm(int? studentId, int formId)
        {
            int resolvedStudentId = ResolveStudentId(studentId);
            int advisorId = ResolveAdvisorId();

            SetSelectedStudentId(resolvedStudentId);

            switch (formId)
            {
                case 1:
                    return RedirectToAction(nameof(Form1), new
                    {
                        studentId = resolvedStudentId,
                        print = true
                    });

                case 2:
                    return RedirectToAction(nameof(Form2), new
                    {
                        studentId = resolvedStudentId,
                        print = true
                    });

                case 3:
                    return RedirectToAction(nameof(Form3), new
                    {
                        studentId = resolvedStudentId,
                        print = true
                    });

                case 4:
                    return RedirectToAction(nameof(Form4), new
                    {
                        studentId = resolvedStudentId,
                        print = true
                    });

                case 5:
                    int form5Id = await GetOrCreateLatestForm5ForStudentAsync(resolvedStudentId, advisorId);

                    return RedirectToAction("Form5", "GraduationProjectEligibility", new
                    {
                        formId = form5Id,
                        print = true
                    });

                default:
                    return NotFound();
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendForm(int studentId, int formId)
        {
            int advisorId = ResolveAdvisorId();
            SetSelectedStudentId(studentId);

            string formType = GetFormTypeFromNumber(formId);

            if (string.IsNullOrWhiteSpace(formType))
                return NotFound();

            string sentStatus = GetSentStatusFromNumber(formId);

            if (formId == 5)
            {
                int form5Id = await GetOrCreateLatestForm5ForStudentAsync(studentId, advisorId);

                var form = await _context.Forms.FirstOrDefaultAsync(f => f.FormId == form5Id);

                if (form != null)
                {
                    form.FormStatus = sentStatus;
                    form.FormDate = DateTime.Now;
                    form.AutoFilled = true;
                    form.AdvisorConfirmation = true;
                }
            }
            else
            {
                var form = await GetOrCreateLatestBaseFormAsync(studentId, advisorId, formType);

                form.FormStatus = sentStatus;
                form.FormDate = DateTime.Now;
                form.AutoFilled = true;
                form.AdvisorConfirmation = true;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Form {formId} sent successfully.";
            return RedirectToAction(nameof(StudentForms), new { studentId });
        }

        [HttpGet]
        public IActionResult StudentFormsByStudent(int studentId)
        {
            SetSelectedStudentId(studentId);
            return RedirectToAction(nameof(StudentForms), new { studentId });
        }
        [HttpGet]
        public async Task<IActionResult> RequestMeeting(int studentId)
        {
            if (HttpContext.Session.GetString("UserRole") != "Advisor")
                return RedirectToAction("Login", "Account");

            int advisorId = ResolveAdvisorId();

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId && s.AdvisorId == advisorId);

            if (student == null)
                return NotFound("Student was not found for this advisor.");

            // نجيب آخر اجتماع بين نفس الطالب ونفس المرشد
            var latestMeeting = await _context.Meetings
                .Where(m => m.StudentId == student.StudentId && m.AdvisorId == advisorId)
                .OrderByDescending(m => m.MeetingId)
                .FirstOrDefaultAsync();

            Db.Meeting meeting;

            // إذا ما فيه اجتماع سابق، أو آخر اجتماع انتهى، ننشئ اجتماع جديد
            if (latestMeeting == null || latestMeeting.RecordingStoppedAt.HasValue || latestMeeting.LastRecordingAction == "stopped")
            {
                meeting = new Db.Meeting
                {
                    StudentId = student.StudentId,
                    AdvisorId = advisorId,
                    StartTime = DateTime.Now,
                    IsRecordingStarted = false,
                    LastRecordingAction = null
                };

                _context.Meetings.Add(meeting);
                await _context.SaveChangesAsync();
            }
            else
            {
                // إذا فيه اجتماع مفتوح، نرجع له بدل ما ننشئ واحد فاضي جديد
                meeting = latestMeeting;
            }

            HttpContext.Session.SetInt32("AdvisorChatStudentId", student.StudentId);
            HttpContext.Session.SetInt32("AdvisorChatMeetingId", meeting.MeetingId);

            return RedirectToAction(nameof(Chat));
        }
        /* ========================================================
                            Community Advisor
           ======================================================== */
        public async Task<IActionResult> CommunityAdvisor()
        {
            // التأكد من الصلاحيات
            if (HttpContext.Session.GetString("UserRole") != "Advisor")
                return RedirectToAction("Login", "Account");

            // التصحيح: استخدام _context بدلاً من _db بناءً على الـ Constructor الخاص بك
            var rawMessages = await _context.CommunityMessages
                .Where(m => m.CommunityId == 1)
                .OrderBy(m => m.MessageDate)
                .ToListAsync();

            var dbMessages = rawMessages.Select(m => new CommunityMessageVM
            {
                SenderName = m.SenderName,
                MessageText = m.MessageText,
                SenderInitials = string.IsNullOrEmpty(m.SenderName) ? "U" :
                                 string.Concat(m.SenderName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                       .Select(n => n[0])).ToUpper(),
                IsAdvisorMessage = m.SenderName != null && m.SenderName.Contains("Amina"),
                BubbleColorClass = m.SenderName != null && m.SenderName.Contains("Amina") ? "msg-purple" : "msg-blue"
            }).ToList();

            // جلب الأعضاء من جدول الطلاب (Students)
            var dbMembers = await _context.Students
                .Select(s => new CommunityMemberVM
                {
                    Name = s.Name, // تأكد أن العمود هو Name كما ظهر في الـ SQL Log الخاص بك
                    ImagePath = "~/images/user.png"
                })
                .ToListAsync();

            var model = new CommunityAdvisorVM
            {
                Messages = dbMessages,
                Members = dbMembers
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SendAdvisorMessage([FromBody] SendAdvisorMessageRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { success = false, message = "الرسالة فارغة" });
            }

            try
            {
                // هنا نثبت اسم الدكتورة كما هو متعارف عليه في النظام عندك
                // أو يمكنك جلبه من السيشن إذا كان مخزناً هناك
                string advisorName = HttpContext.Session.GetString("UserName") ?? "Dr. Amina Hasan Gamlo";

                var newMessage = new Acadify.Models.Db.CommunityMessage
                {
                    CommunityId = 1,
                    SenderName = advisorName,
                    MessageText = request.Message.Trim(),
                    MessageDate = DateTime.Now
                };

                _context.CommunityMessages.Add(newMessage);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    text = newMessage.MessageText,
                    senderName = newMessage.SenderName
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        private async Task AddRecommendationNotificationToStudentAsync(
    int studentId,
    string type,
    string message)
        {
            var notification = new Db.Notification
            {
                SenderRole = "Advisor",
                SourceType = "Recommendation",
                Type = type,
                Message = message,

                // This notification is for the student only.
                StudentId = studentId,
                AdvisorId = null,
                AdminId = null,

                Date = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        /* ========================================================
                            Form 1
           ======================================================== */
        [HttpGet]
        public async Task<IActionResult> Form1(int? studentId = null)
        {
            if (studentId.HasValue && studentId.Value > 0)
                SetSelectedStudentId(studentId.Value);

            int resolvedStudentId = ResolveStudentId(studentId);
            int advisorId = ResolveAdvisorId();

            var latestForm = await _context.Forms
                .Where(f => f.StudentId == resolvedStudentId && f.FormType == "Form 1")
                .OrderByDescending(f => f.FormDate)
                .ThenByDescending(f => f.FormId)
                .FirstOrDefaultAsync();

            Form1ViewModel model;

            if (latestForm != null && TryDeserializeForm1Snapshot(latestForm.AdvisorNotes, out var savedModel))
            {
                model = savedModel;
                model.Status = latestForm.FormStatus ?? model.Status;
            }
            else
            {
                model = await CreateForm1FromDatabase(resolvedStudentId);
            }

            model.StudentId = resolvedStudentId.ToString();

            return View(model);
        }

        private async Task<Form1ViewModel> CreateForm1FromDatabase(int studentId)
        {
            // جلب بيانات الطالب مع اليوزر والمشرف
            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Advisor)
                    .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            // جلب آخر ترانسكربت للطالب عشان نأخذ منه المعدل
            var transcript = await _context.Transcripts
                .FirstOrDefaultAsync(t => t.StudentId == studentId);

            // تعبئة Form 1 من الداتابيس بدل البيانات الافتراضية
            return new Form1ViewModel
            {
                FullName = student?.User?.Name ?? "",
                StudentId = studentId.ToString(),
                Email = student?.User?.Email ?? "",

                // Student.cs ما فيه MobilePhone، لذلك نخليه فاضي
                MobilePhone = "",

                // المعدل من جدول Transcript، العمود عندك اسمه Gpa
                GpaCurrent = transcript?.Gpa?.ToString("0.00") ?? "",

                // اسم المشرف من Advisor ثم User
                AdvisorName = student?.Advisor?.User?.Name ?? "",

                // هذه التواريخ تنحط بتاريخ اليوم
                ApprovalDate = DateTime.Today,
                AdvisingCommencementDate = DateTime.Today,

                Status = "Draft"
            };
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveForm1(Form1ViewModel model)
        {
            int studentId = await UpdateForm1Async(model, "Saved", false);

            TempData["SuccessMessage"] = "Form 1 saved successfully.";
            return RedirectToAction(nameof(Form1), new { studentId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendForm1(Form1ViewModel model)
        {
            int studentId = await UpdateForm1Async(model, "Sent to Advising Committee", true);

            TempData["SuccessMessage"] = "Form 1 sent successfully.";
            return RedirectToAction(nameof(Form1), new { studentId });
        }



        private async Task<int> UpdateForm1Async(Form1ViewModel model, string status, bool advisorConfirmation)
        {
            int studentId = ResolveStudentId(ParseNullableInt(model.StudentId));
            int advisorId = ResolveAdvisorId();

            var form = await GetOrCreateLatestBaseFormAsync(studentId, advisorId, "Form 1");

            Form1ViewModel existingModel;

            if (!TryDeserializeForm1Snapshot(form.AdvisorNotes, out existingModel))
            {
                existingModel = await CreateForm1FromDatabase(studentId);
            }

            var finalModel = MergeForm1Model(existingModel, model);
            finalModel.StudentId = studentId.ToString();
            finalModel.Status = status;

            form.FormStatus = status;
            form.FormDate = DateTime.Now;
            form.AutoFilled = true;
            form.AdvisorConfirmation = advisorConfirmation;

            // نخزن نسخة الفورم كاملة عشان التعديلات ترجع تظهر بعد تحديث الصفحة
            form.AdvisorNotes = JsonSerializer.Serialize(finalModel);

            await _context.SaveChangesAsync();

            return studentId;
        }

        private static bool TryDeserializeForm1Snapshot(string? json, out Form1ViewModel model)
        {
            model = new Form1ViewModel();

            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                var result = JsonSerializer.Deserialize<Form1ViewModel>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (result == null)
                    return false;

                model = result;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Form1ViewModel MergeForm1Model(Form1ViewModel oldModel, Form1ViewModel newModel)
        {
            return new Form1ViewModel
            {
                FullName = KeepNewValue(newModel.FullName, oldModel.FullName),
                StudentId = KeepNewValue(newModel.StudentId, oldModel.StudentId),
                Email = KeepNewValue(newModel.Email, oldModel.Email),
                MobilePhone = KeepNewValue(newModel.MobilePhone, oldModel.MobilePhone),
                GpaCurrent = KeepNewValue(newModel.GpaCurrent, oldModel.GpaCurrent),
                AdvisorName = KeepNewValue(newModel.AdvisorName, oldModel.AdvisorName),
                MedicalNeedsOptional = KeepNewValue(newModel.MedicalNeedsOptional, oldModel.MedicalNeedsOptional),

                ApprovalDate = newModel.ApprovalDate == default
                    ? oldModel.ApprovalDate
                    : newModel.ApprovalDate,

                AdvisingCommencementDate = newModel.AdvisingCommencementDate == default
                    ? oldModel.AdvisingCommencementDate
                    : newModel.AdvisingCommencementDate,

                Status = KeepNewValue(newModel.Status, oldModel.Status)
            };
        }

        private static string KeepNewValue(string? newValue, string? oldValue)
        {
            return string.IsNullOrWhiteSpace(newValue)
                ? oldValue ?? ""
                : newValue;
        }

        /* ========================================================
                            Form 2
           ======================================================== */

        [HttpGet]
        public async Task<IActionResult> Form2(int? studentId = null)
        {
            // تحديد الطالب الحالي
            int resolvedStudentId = ResolveStudentId(studentId);

            // تحديد المرشد الحالي
            int advisorId = ResolveAdvisorId();

            // جلب بيانات الطالب
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StudentId == resolvedStudentId);

            if (student == null)
                return NotFound("Student not found.");

            // جلب الترانسكربت مع المواد المرتبطة به
            var transcript = await _context.Transcripts
                .Include(t => t.Courses)
                .FirstOrDefaultAsync(t => t.StudentId == resolvedStudentId);

            // قراءة مواد الترانسكربت من ExtractedCourses أو ExtractedInfo
            var transcriptCourses = ReadTranscriptCourses(
                transcript?.ExtractedCourses,
                transcript?.ExtractedInfo
            );

            // مواد الترانسكربت المرتبطة من قاعدة البيانات كـ fallback
            var transcriptDbCourses = transcript?.Courses?.ToList() ?? new List<Db.Course>();

            // مواد الحذف / الرسوب
            var dropSubjects = transcriptCourses
                .Where(c =>
                {
                    var grade = c.Grade?.Trim().ToUpper();
                    return grade == "W" || grade == "F" || grade == "WF";
                })
                .Where(c => !string.IsNullOrWhiteSpace(c.CourseId))
                .Select(c => NormalizeCourseId(c.CourseId))
                .Distinct()
                .ToList();

            // مواد IC
            var icSubjects = transcriptCourses
                .Where(c => (c.Grade?.Trim().ToUpper()) == "IC")
                .Where(c => !string.IsNullOrWhiteSpace(c.CourseId))
                .Select(c => NormalizeCourseId(c.CourseId))
                .Distinct()
                .ToList();

            // مواد IP
            var ipSubjects = transcriptCourses
                .Where(c => (c.Grade?.Trim().ToUpper()) == "IP")
                .Where(c => !string.IsNullOrWhiteSpace(c.CourseId))
                .Select(c => NormalizeCourseId(c.CourseId))
                .Distinct()
                .ToList();

            // أكواد المواد من الترانسكربت المقروء
            var parsedCourseIds = transcriptCourses
                .Where(c => !string.IsNullOrWhiteSpace(c.CourseId))
                .Select(c => NormalizeCourseId(c.CourseId))
                .Distinct()
                .ToList();

            // أكواد المواد من جدول Transcript.Courses
            var dbCourseIds = transcriptDbCourses
                .Where(c => !string.IsNullOrWhiteSpace(c.CourseId))
                .Select(c => NormalizeCourseId(c.CourseId))
                .Distinct()
                .ToList();

            // دمج أكواد المواد
            var allCourseIds = parsedCourseIds
                .Concat(dbCourseIds)
                .Distinct()
                .ToList();

            // جلب ساعات المواد من جدول Courses
            var courseHours = allCourseIds.Any()
                ? await _context.Courses
                    .Where(c => allCourseIds.Contains(c.CourseId.Trim().ToUpper()))
                    .ToDictionaryAsync(
                        c => c.CourseId.Trim().ToUpper(),
                        c => c.Hours
                    )
                : new Dictionary<string, int>();

            // المواد المجتازة حسب الدرجات
            var passedCourseIds = transcriptCourses
                .Where(c => IsPassedGrade(c.Grade))
                .Where(c => !string.IsNullOrWhiteSpace(c.CourseId))
                .Select(c => NormalizeCourseId(c.CourseId))
                .Distinct()
                .ToList();

            // حساب الساعات المكتسبة من النص إذا موجودة
            int completedHours = ExtractTranscriptEarnedHours(transcript?.ExtractedInfo);

            // إذا ما قدر يقرأ الساعات من النص، يحسبها من المواد المجتازة
            if (completedHours <= 0 && passedCourseIds.Any())
            {
                completedHours = passedCourseIds.Sum(id =>
                    courseHours.ContainsKey(id) ? courseHours[id] : 0
                );
            }

            // إذا ما فيه درجات مقروءة، يستخدم مواد الترانسكربت المرتبطة من قاعدة البيانات
            if (completedHours <= 0 && transcriptDbCourses.Any())
            {
                completedHours = transcriptDbCourses.Sum(c => c.Hours);
            }

            // استخراج آخر فصل من الترانسكربت
            var semester = ExtractLastAcademicTerm(transcript?.ExtractedInfo);
            if (semester == "غير محدد")
                semester = "";

            var comingSemester = GetNextSemesterText(semester);

            // جلب آخر Form 2 للطالب
            var latestForm = await _context.Forms
                .Include(f => f.CourseChoiceMonitoringForm)
                .Where(f => f.StudentId == resolvedStudentId && f.FormType == "Form 2")
                .OrderByDescending(f => f.FormId)
                .FirstOrDefaultAsync();

            // إذا لا يوجد Form 2، ننشئ واحد جديد
            if (latestForm == null)
            {
                latestForm = new Db.Form
                {
                    StudentId = resolvedStudentId,
                    AdvisorId = advisorId,
                    FormType = "Form 2",
                    FormDate = DateTime.Now,
                    FormStatus = "Draft",
                    AdvisorNotes = null,
                    AutoFilled = true,
                    AdvisorConfirmation = false
                };

                _context.Forms.Add(latestForm);
                await _context.SaveChangesAsync();
            }

            // إذا تفاصيل Form 2 غير موجودة، ننشئها
            if (latestForm.CourseChoiceMonitoringForm == null)
            {
                latestForm.CourseChoiceMonitoringForm = new Db.CourseChoiceMonitoringForm
                {
                    FormId = latestForm.FormId,
                    SelectedCoursesJson = "[]"
                };

                _context.CourseChoiceMonitoringForms.Add(latestForm.CourseChoiceMonitoringForm);
                await _context.SaveChangesAsync();
            }

            var form2 = latestForm.CourseChoiceMonitoringForm;

            // مهم:
            // هنا نقرأ المواد المختارة من Course Recommendation فقط
            // لا نولّد توصيات جديدة داخل Form 2
            var selectedCourses = SafeDeserializeSelectedCourses(form2.SelectedCoursesJson);

            // حساب الساعات المختارة من المواد المحفوظة
            int advisedCreditHours = CalculateSelectedCoursesHours(selectedCourses);

            if (string.IsNullOrWhiteSpace(form2.Semester))
                form2.Semester = semester;

            if (string.IsNullOrWhiteSpace(form2.ComingSemester))
                form2.ComingSemester = comingSemester;

            if (!form2.RunningCreditHours.HasValue || form2.RunningCreditHours == 0)
                form2.RunningCreditHours = completedHours;

            if (!form2.AdvisedCreditHours.HasValue || form2.AdvisedCreditHours == 0)
                form2.AdvisedCreditHours = advisedCreditHours;

            if (string.IsNullOrWhiteSpace(form2.Level))
                form2.Level = CalculateLevelFromCompletedHours(completedHours);

            if (string.IsNullOrWhiteSpace(form2.DropSubjects))
                form2.DropSubjects = string.Join(", ", dropSubjects);

            if (string.IsNullOrWhiteSpace(form2.ICSubjects))
                form2.ICSubjects = string.Join(", ", icSubjects);

            if (string.IsNullOrWhiteSpace(form2.IpSubjects))
                form2.IpSubjects = string.Join(", ", ipSubjects);



            await _context.SaveChangesAsync();

            // تجهيز ViewModel للعرض
            var model = new Form2ViewModel
            {
                StudentName = student.User?.Name ?? student.Name ?? "",
                StudentId = student.StudentId,
                Semester = form2.Semester ?? "",
                ComingSemester = form2.ComingSemester ?? "",
                RunningCreditHours = form2.RunningCreditHours ?? 0,
                AdvisedCreditHours = form2.AdvisedCreditHours ?? 0,
                Level = form2.Level ?? "",
                DropSubjects = form2.DropSubjects ?? "",
                ICSubjects = form2.ICSubjects ?? "",
                IPSubjects = form2.IpSubjects ?? "",
                SelectedCourses = selectedCourses
            };

            ViewBag.AdvisorNotes = latestForm.AdvisorNotes ?? "";
            return View(model);
        }


        private string CalculateLevelFromCompletedHours(int completedHours)
        {
            if (completedHours <= 0)
                return "";

            if (completedHours < 20)
                return "Level 1";

            if (completedHours < 40)
                return "Level 2";

            if (completedHours < 60)
                return "Level 3";

            if (completedHours < 80)
                return "Level 4";

            if (completedHours < 100)
                return "Level 5";

            if (completedHours < 120)
                return "Level 6";

            return "Level 7";
        }

        private List<TranscriptCourseItem> ReadTranscriptCourses(string? extractedCourses, string? extractedInfo)
        {
            var result = new List<TranscriptCourseItem>();

            // 1. نحاول نقرأ ExtractedCourses كـ JSON
            result = TryReadTranscriptCoursesJson(extractedCourses);
            if (result.Any())
                return result;

            // 2. إذا ExtractedCourses فيه نص عادي، نحاول نستخرج منه
            result = TryParseTranscriptCoursesFromText(extractedCourses);
            if (result.Any())
                return result;

            // 3. إذا ما نفع، نحاول من ExtractedInfo
            result = TryParseTranscriptCoursesFromText(extractedInfo);
            if (result.Any())
                return result;

            return new List<TranscriptCourseItem>();
        }

        private List<TranscriptCourseItem> TryReadTranscriptCoursesJson(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<TranscriptCourseItem>();

            var cleanedText = text.Trim();

            // بعض مخرجات الـ API ممكن يكون فيها كلام قبل وبعد JSON
            var startIndex = cleanedText.IndexOf('[');
            var endIndex = cleanedText.LastIndexOf(']');

            if (startIndex == -1 || endIndex == -1 || endIndex <= startIndex)
                return new List<TranscriptCourseItem>();

            var jsonText = cleanedText.Substring(startIndex, endIndex - startIndex + 1);

            try
            {
                return JsonSerializer.Deserialize<List<TranscriptCourseItem>>(
                    jsonText,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                ) ?? new List<TranscriptCourseItem>();
            }
            catch (JsonException)
            {
                return new List<TranscriptCourseItem>();
            }
        }

        private List<TranscriptCourseItem> TryParseTranscriptCoursesFromText(string? text)
        {
            var result = new List<TranscriptCourseItem>();

            if (string.IsNullOrWhiteSpace(text))
                return result;

            var lines = text
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => Regex.Replace(l, @"\s+", " ").Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            var courseRegex = new Regex(
                @"\b([A-Z]{2,6})\s*-?\s*(\d{3})\b",
                RegexOptions.IgnoreCase);



            var gradeRegex = new Regex(
                @"(?<![A-Z])(A\+|B\+|C\+|D\+|WF|IC|IP|NP|TR|A|B|C|D|F|W|P)(?![A-Z])",
                RegexOptions.IgnoreCase);

            foreach (var line in lines)
            {
                var courseMatch = courseRegex.Match(line);
                if (!courseMatch.Success)
                    continue;

                var courseId = NormalizeCourseId(courseMatch.Groups[1].Value + courseMatch.Groups[2].Value);

                var grades = gradeRegex.Matches(line)
                    .Select(m => m.Value.ToUpper())
                    .ToList();

                // نأخذ آخر Grade في السطر لأن بداية اسم المادة ممكن تحتوي كلمات كثيرة
                var grade = grades.LastOrDefault() ?? "";

                if (string.IsNullOrWhiteSpace(courseId))
                    continue;

                if (result.Any(c => NormalizeCourseId(c.CourseId) == courseId))
                    continue;

                result.Add(new TranscriptCourseItem
                {
                    CourseId = courseId,
                    Grade = grade
                });
            }

            return result;
        }

        private static string NormalizeCourseId(string? courseId)
        {
            if (string.IsNullOrWhiteSpace(courseId))
                return "";

            return courseId
                .Replace("-", "")
                .Replace(" ", "")
                .Trim()
                .ToUpper();
        }

        private static bool IsPassedGrade(string? grade)
        {
            if (string.IsNullOrWhiteSpace(grade))
                return false;

            grade = grade.Trim().ToUpper();

            return grade == "A+" ||
                   grade == "A" ||
                   grade == "B+" ||
                   grade == "B" ||
                   grade == "C+" ||
                   grade == "C" ||
                   grade == "D+" ||
                   grade == "D" ||
                   grade == "P" ||
                   grade == "NP" ||
                   grade == "TR";
        }

        private List<SelectedCourseVM> SafeDeserializeSelectedCourses(string? selectedCoursesJson)
        {
            if (string.IsNullOrWhiteSpace(selectedCoursesJson))
                return new List<SelectedCourseVM>();

            try
            {
                return JsonSerializer.Deserialize<List<SelectedCourseVM>>(
                    selectedCoursesJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                ) ?? new List<SelectedCourseVM>();
            }
            catch (JsonException)
            {
                return new List<SelectedCourseVM>();
            }
        }

        private async Task<List<SelectedCourseVM>> BuildForm2RecommendedCoursesAsync(
            List<string> passedCourseIds,
            List<string> dropSubjects,
            List<string> icSubjects,
            List<string> ipSubjects)
        {
            // المواد التي لا نقترحها لأنها مجتازة أو مسجلة حاليًا
            var excludedIds = passedCourseIds
                .Concat(ipSubjects)
                .Distinct()
                .ToList();

            // مواد يجب إعادة النظر فيها مثل F/W/WF/IC
            var priorityIds = dropSubjects
                .Concat(icSubjects)
                .Distinct()
                .ToList();

            var priorityCourses = priorityIds.Any()
                ? await _context.Courses
                    .Where(c => priorityIds.Contains(c.CourseId.Trim().ToUpper()))
                    .ToListAsync()
                : new List<Db.Course>();

            // مواد خطة نظم المعلومات
            var planCoursesRaw = await (
                from spc in _context.StudyPlanCourses
                join p in _context.StudyPlans on spc.PlanId equals p.PlanId
                join c in _context.Courses on spc.CourseId equals c.CourseId
                where p.Major != null &&
                      p.Major.Trim().ToUpper() == "INFORMATION SYSTEMS"
                select c
            ).ToListAsync();

            var planCourses = planCoursesRaw
                .GroupBy(c => NormalizeCourseId(c.CourseId))
                .Select(g => g.First())
                .OrderBy(c => GetCourseNumberForSort(c.CourseId))
                .ThenBy(c => c.CourseId)
                .ToList();

            var candidateCourses = priorityCourses
                .Concat(planCourses)
                .Where(c => !string.IsNullOrWhiteSpace(c.CourseId))
                .Where(c => !excludedIds.Contains(NormalizeCourseId(c.CourseId)))
                .GroupBy(c => NormalizeCourseId(c.CourseId))
                .Select(g => g.First())
                .ToList();

            // اختيار مواد تقريبًا بين 15 و18 ساعة
            var selectedDbCourses = new List<Db.Course>();
            int totalHours = 0;

            foreach (var course in candidateCourses)
            {
                if (course.Hours <= 0)
                    continue;

                if (totalHours + course.Hours > 18 && selectedDbCourses.Any())
                    continue;

                selectedDbCourses.Add(course);
                totalHours += course.Hours;

                if (totalHours >= 15)
                    break;
            }

            return ConvertCoursesToSelectedCourseVMs(selectedDbCourses);
        }

        private static int GetCourseNumberForSort(string? courseId)
        {
            if (string.IsNullOrWhiteSpace(courseId))
                return 9999;

            var match = Regex.Match(courseId, @"\d{3}");
            if (match.Success && int.TryParse(match.Value, out int number))
                return number;

            return 9999;
        }

        private List<SelectedCourseVM> ConvertCoursesToSelectedCourseVMs(List<Db.Course> courses)
        {
            var result = new List<SelectedCourseVM>();

            foreach (var course in courses)
            {
                var item = new SelectedCourseVM();

                // نحط أكثر من اسم محتمل عشان لو ViewModel عندكم يستخدم اسم مختلف
                SetPropertyIfExists(item, "CourseId", course.CourseId);
                SetPropertyIfExists(item, "CourseCode", course.CourseId);
                SetPropertyIfExists(item, "Code", course.CourseId);

                SetPropertyIfExists(item, "CourseName", course.CourseName);
                SetPropertyIfExists(item, "Name", course.CourseName);

                SetPropertyIfExists(item, "Hours", course.Hours);
                SetPropertyIfExists(item, "CreditHours", course.Hours);
                SetPropertyIfExists(item, "CourseHours", course.Hours);

                result.Add(item);
            }

            return result;
        }

        private static void SetPropertyIfExists(object target, string propertyName, object? value)
        {
            if (target == null || value == null)
                return;

            var property = target.GetType().GetProperty(propertyName);

            if (property == null || !property.CanWrite)
                return;

            try
            {
                var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                var convertedValue = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
                property.SetValue(target, convertedValue);
            }
            catch
            {
                // إذا الخاصية نوعها مختلف، نتجاهلها بدون ما نوقف الصفحة
            }
        }

        private int CalculateSelectedCoursesHours(List<SelectedCourseVM> selectedCourses)
        {
            if (selectedCourses == null || selectedCourses.Count == 0)
                return 0;

            int total = 0;

            foreach (var item in selectedCourses)
            {
                var type = item.GetType();

                var possibleHourProperties = new[]
                {
            "Hours",
            "CreditHours",
            "CourseHours"
        };

                foreach (var propName in possibleHourProperties)
                {
                    var prop = type.GetProperty(propName);
                    if (prop == null)
                        continue;

                    var value = prop.GetValue(item);

                    if (value != null && int.TryParse(value.ToString(), out int hours))
                    {
                        total += hours;
                        break;
                    }
                }
            }

            return total;
        }

        private static string GetNextSemesterText(string? currentSemester)
        {
            if (string.IsNullOrWhiteSpace(currentSemester))
                return "";

            var match = Regex.Match(
                currentSemester,
                @"\b(FALL|SPRING|SUMMER|WINTER)\s+(\d{4})/(\d{4})\b",
                RegexOptions.IgnoreCase);

            if (!match.Success)
                return "";

            var term = match.Groups[1].Value.ToUpper();
            int firstYear = int.Parse(match.Groups[2].Value);
            int secondYear = int.Parse(match.Groups[3].Value);

            if (term == "FALL")
                return $"SPRING {firstYear}/{secondYear}";

            if (term == "SPRING")
                return $"SUMMER {firstYear}/{secondYear}";

            if (term == "SUMMER")
                return $"FALL {secondYear}/{secondYear + 1}";

            return "";
        }

       


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveForm2(Form2ViewModel model)
        {
            await UpdateForm2Async(model, "Saved");

            TempData["SuccessMessage"] = "Form 2 saved successfully.";
            return RedirectToAction(nameof(Form2), new { studentId = model.StudentId });
        }
        [HttpPost]
        public async Task<IActionResult> SendForm2(Form2ViewModel model)
        {
            await UpdateForm2Async(model, "Sent to Advising Committee");

            await AddRecommendationNotificationToStudentAsync(
                model.StudentId,
                "course recommendation approved by advisor",
                "Your advisor approved the course recommendation and sent Form 2 to the advising committee.");

            TempData["SuccessMessage"] = "Form 2 sent successfully.";
            return RedirectToAction(nameof(Form2), new { studentId = model.StudentId });
        }

        [HttpPost]
        public async Task<IActionResult> ReturnForm2ToStudent(Form2ViewModel model)
        {
            await UpdateForm2Async(model, "Returned to Student");

            await AddRecommendationNotificationToStudentAsync(
                model.StudentId,
                "course recommendation returned by advisor",
                "Your advisor returned the course recommendation. Please review and update it.");

            TempData["SuccessMessage"] = "Form 2 returned to the student successfully.";
            return RedirectToAction(nameof(Form2), new { studentId = model.StudentId });
        }

        private async Task UpdateForm2Async(Form2ViewModel model, string status)
        {
            int studentId = model.StudentId;

            var latestForm = await _context.Forms
                .Include(f => f.CourseChoiceMonitoringForm)
                .Where(f => f.StudentId == studentId && f.FormType == "Form 2")
                .OrderByDescending(f => f.FormDate)
                .ThenByDescending(f => f.FormId)
                .FirstOrDefaultAsync();

            if (latestForm == null || latestForm.CourseChoiceMonitoringForm == null)
                throw new InvalidOperationException("Form 2 not found.");

            var form2 = latestForm.CourseChoiceMonitoringForm;

            form2.Semester = model.Semester;
            form2.ComingSemester = model.ComingSemester;
            form2.RunningCreditHours = model.RunningCreditHours;
            form2.AdvisedCreditHours = model.AdvisedCreditHours;
            form2.Level = model.Level;
            form2.DropSubjects = model.DropSubjects;
            form2.ICSubjects = model.ICSubjects;
            form2.IpSubjects = model.IPSubjects;

            // لا نغير SelectedCoursesJson هنا
            // لأنها جاية من Course Recommendation ومحفوظة مسبقًا

            if (Request.Form.ContainsKey("AdvisorNotes"))
            {
                latestForm.AdvisorNotes = Request.Form["AdvisorNotes"].ToString();
            }

            latestForm.FormStatus = status;
            latestForm.FormDate = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        /* ========================================================
                            Form 3
           ======================================================== */

        [HttpGet]
        public async Task<IActionResult> Form3(int? studentId = null, int? meetingId = null)
        {
            if (HttpContext.Session.GetString("UserRole") != "Advisor")
                return RedirectToAction("Login", "Account");

            int advisorId = ResolveAdvisorId();
            int resolvedStudentId = ResolveStudentId(studentId);

            var student = await _context.Students
                .FirstOrDefaultAsync(s =>
                    s.StudentId == resolvedStudentId &&
                    s.AdvisorId == advisorId);

            if (student == null)
                return NotFound("Student was not found for this advisor.");

            Db.Meeting? meeting = null;

            if (meetingId.HasValue)
            {
                meeting = await _context.Meetings
                    .FirstOrDefaultAsync(m =>
                        m.MeetingId == meetingId.Value &&
                        m.StudentId == student.StudentId &&
                        m.AdvisorId == advisorId);
            }

            if (meeting == null)
            {
                int? sessionMeetingId = HttpContext.Session.GetInt32("AdvisorChatMeetingId");

                if (sessionMeetingId.HasValue)
                {
                    meeting = await _context.Meetings
                        .FirstOrDefaultAsync(m =>
                            m.MeetingId == sessionMeetingId.Value &&
                            m.StudentId == student.StudentId &&
                            m.AdvisorId == advisorId);
                }
            }

            if (meeting == null)
            {
                meeting = await _context.Meetings
                    .Where(m => m.StudentId == student.StudentId && m.AdvisorId == advisorId)
                    .OrderByDescending(m => m.MeetingId)
                    .FirstOrDefaultAsync();
            }

            if (meeting == null)
            {
                meeting = new Db.Meeting
                {
                    StudentId = student.StudentId,
                    AdvisorId = advisorId,
                    StartTime = DateTime.Now,
                    IsRecordingStarted = false,
                    LastRecordingAction = null
                };

                _context.Meetings.Add(meeting);
                await _context.SaveChangesAsync();
            }

            HttpContext.Session.SetInt32("AdvisorChatMeetingId", meeting.MeetingId);

            var existingForm = await _context.Forms
                .Include(f => f.MeetingForm)
                .Where(f => f.FormType == "Form 3"
                         && f.StudentId == student.StudentId
                         && f.AdvisorId == advisorId
                         && f.MeetingForm != null
                         && f.MeetingForm.MeetingId == meeting.MeetingId)
                .OrderByDescending(f => f.FormDate)
                .FirstOrDefaultAsync();

            var model = CreateEmptyForm3ViewModel();

            model.MeetingId = meeting.MeetingId;
            model.StudentName = student.Name;
            model.StudentId = student.StudentId.ToString();
            model.Status = existingForm?.FormStatus ?? "Draft";
            model.AdvisorNotes = existingForm?.AdvisorNotes ?? "";

            var row1 = model.Meetings[0];

            if (existingForm?.MeetingForm != null)
            {
                var mf = existingForm.MeetingForm;

                if (mf.MeetingStart.HasValue)
                    row1.MeetingDate = mf.MeetingStart.Value.ToString("dd/MM/yyyy hh:mm tt");

                row1.PurposeAcademic = mf.MeetingPurpose == "Academic";
                row1.PurposeCareer = mf.MeetingPurpose == "Career";
                row1.PurposeOther = mf.MeetingPurpose == "Other";
                row1.ReferralName = mf.ReferredTo ?? "";
                row1.ReferralReason = mf.ReferralReason ?? "";
                row1.ProposedSolutions = mf.MeetingNotes ?? "";
            }

            if (string.IsNullOrWhiteSpace(row1.MeetingDate) && meeting.RecordingStartedAt.HasValue)
                row1.MeetingDate = meeting.RecordingStartedAt.Value.ToString("dd/MM/yyyy hh:mm tt");

            if (string.IsNullOrWhiteSpace(row1.ProposedSolutions) &&
                !string.IsNullOrWhiteSpace(meeting.ChatSummary))
            {
                row1.ProposedSolutions = meeting.ChatSummary;
            }

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> SaveForm3(Form3ViewModel model)
        {
            await SaveOrSendForm3Async(model, "Draft");

            TempData["Success"] = "Form 3 saved successfully.";
            return RedirectToAction(nameof(Form3), new { studentId = model.StudentId });
        }

        [HttpPost]
        public async Task<IActionResult> SendForm3(Form3ViewModel model)
        {
            await SaveOrSendForm3Async(model, "Sent");

            TempData["Success"] = "Form 3 sent successfully.";
            return RedirectToAction(nameof(Form3), new { studentId = model.StudentId });
        }

       

        [HttpPost]
        public IActionResult AddNotesForm3(string notes)
        {
            TempData["Success"] = "Notes saved successfully.";
            return RedirectToAction(nameof(Form3), new { studentId = GetSelectedStudentId() });
        }

        private Form3ViewModel CreateEmptyForm3ViewModel()
        {
            var model = new Form3ViewModel
            {
                StudentName = "",
                StudentId = "",
                Status = "Draft",
                AdvisorNotes = "",
                Meetings = new List<Form3MeetingRowVM>()
            };

            for (int i = 1; i <= 3; i++)
            {
                model.Meetings.Add(new Form3MeetingRowVM
                {
                    MeetingNo = i,
                    MeetingDate = "",
                    PurposeAcademic = false,
                    PurposeCareer = false,
                    PurposeOther = false,
                    ReferralName = "",
                    ReferralReason = "",
                    ProposedSolutions = "",
                    StudentInitial = "",
                    AdvisorInitial = ""
                });
            }

            return model;
        }

        private async Task SaveOrSendForm3Async(Form3ViewModel model, string status)
        {
            int advisorId = ResolveAdvisorId();
            int studentId = ResolveStudentId(ParseNullableInt(model.StudentId));

            var meeting = await _context.Meetings
                .FirstOrDefaultAsync(m =>
                    m.MeetingId == model.MeetingId &&
                    m.StudentId == studentId &&
                    m.AdvisorId == advisorId);

            if (meeting == null)
                throw new InvalidOperationException("Meeting not found.");

            var form = await _context.Forms
                .Include(f => f.MeetingForm)
                .FirstOrDefaultAsync(f => f.FormType == "Form 3"
                                      && f.StudentId == studentId
                                      && f.AdvisorId == advisorId
                                      && f.MeetingForm != null
                                      && f.MeetingForm.MeetingId == meeting.MeetingId);

            if (form == null)
            {
                form = new Db.Form
                {
                    StudentId = studentId,
                    AdvisorId = advisorId,
                    FormType = "Form 3",
                    FormDate = DateTime.Now,
                    FormStatus = status,
                    AdvisorNotes = model.AdvisorNotes,
                    AutoFilled = true,
                    AdvisorConfirmation = status == "Sent"
                };

                _context.Forms.Add(form);
                await _context.SaveChangesAsync();

                form.MeetingForm = new Db.MeetingForm
                {
                    FormId = form.FormId,
                    MeetingId = meeting.MeetingId
                };

                _context.MeetingForms.Add(form.MeetingForm);
            }

            form.FormDate = DateTime.Now;
            form.FormStatus = status;
            form.AdvisorNotes = model.AdvisorNotes;
            form.AdvisorConfirmation = status == "Sent";

            var row1 = model.Meetings.FirstOrDefault();

            if (row1 != null && form.MeetingForm != null)
            {
                if (DateTime.TryParseExact(
                        row1.MeetingDate,
                        "dd/MM/yyyy hh:mm tt",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var parsedMeetingStart))
                {
                    form.MeetingForm.MeetingStart = parsedMeetingStart;
                }
                else
                {
                    form.MeetingForm.MeetingStart = meeting.RecordingStartedAt;
                }

                form.MeetingForm.MeetingEnd = meeting.RecordingStoppedAt;

                if (row1.PurposeAcademic)
                    form.MeetingForm.MeetingPurpose = "Academic";
                else if (row1.PurposeCareer)
                    form.MeetingForm.MeetingPurpose = "Career";
                else if (row1.PurposeOther)
                    form.MeetingForm.MeetingPurpose = "Other";
                else
                    form.MeetingForm.MeetingPurpose = null;

                form.MeetingForm.ReferredTo = row1.ReferralName;
                form.MeetingForm.ReferralReason = row1.ReferralReason;
                form.MeetingForm.MeetingNotes = row1.ProposedSolutions;
                form.MeetingForm.MeetingId = meeting.MeetingId;
            }

            await _context.SaveChangesAsync();
        }

        [HttpGet]
        public async Task<IActionResult> Form3History(int? studentId = null)
        {
            if (HttpContext.Session.GetString("UserRole") != "Advisor")
                return RedirectToAction("Login", "Account");

            int advisorId = ResolveAdvisorId();
            int resolvedStudentId = ResolveStudentId(studentId);

            var items = await _context.Forms
                .Include(f => f.MeetingForm)
                .Where(f =>
                    f.FormType == "Form 3" &&
                    f.StudentId == resolvedStudentId &&
                    f.AdvisorId == advisorId &&
                    f.MeetingForm != null)
                .OrderByDescending(f => f.FormDate)
                .Select(f => new FormHistoryItemViewModel
                {
                    FormId = f.FormId,
                    FormTitle = "Meeting Record (Form 3)",
                    Status = f.FormStatus,
                    DateText = f.FormDate.ToString("MMM d, yyyy"),
                    ViewUrl = Url.Action("ViewSavedForm3", "Advisor", new { formId = f.FormId })!
                })
                .ToListAsync();

            return View(items);
        }

        [HttpGet]
        public IActionResult ViewSavedForm3(int formId)
        {
            var form = _context.Forms
                .Include(f => f.MeetingForm)
                .FirstOrDefault(f => f.FormId == formId && f.FormType == "Form 3");

            if (form == null || form.MeetingForm == null)
                return NotFound();

            var model = CreateEmptyForm3ViewModel();
            model.Status = form.FormStatus ?? "Draft";
            model.AdvisorNotes = form.AdvisorNotes ?? "";

            var row1 = model.Meetings[0];
            var mf = form.MeetingForm;

            if (mf.MeetingStart.HasValue)
                row1.MeetingDate = mf.MeetingStart.Value.ToString("dd/MM/yyyy hh:mm tt");

            row1.PurposeAcademic = mf.MeetingPurpose == "Academic";
            row1.PurposeCareer = mf.MeetingPurpose == "Career";
            row1.PurposeOther = mf.MeetingPurpose == "Other";
            row1.ReferralName = mf.ReferredTo ?? "";
            row1.ReferralReason = mf.ReferralReason ?? "";
            row1.ProposedSolutions = mf.MeetingNotes ?? "";

            return View("Form3", model);
        }

        /* ========================================================
                            Form 4
           ======================================================== */

        [HttpGet]
        public async Task<IActionResult> Form4(int? studentId = null)
        {
            int resolvedStudentId = ResolveStudentId(studentId);

            var student = await _context.Students
                .Include(s => s.Transcript)
                    .ThenInclude(t => t.Courses)
                .FirstOrDefaultAsync(s => s.StudentId == resolvedStudentId);

            if (student == null)
                return NotFound("Student not found.");

            var model = BuildForm4ViewModel(student);
            model.PlanCourseOptions = GetIsPlanCourseOptions();

            var latestForm = await _context.Forms
                .Where(f => f.StudentId == resolvedStudentId && f.FormType == "Form 4")
                .OrderByDescending(f => f.FormId)
                .FirstOrDefaultAsync();

            if (latestForm != null)
                model.AdvisorNotes = latestForm.AdvisorNotes ?? "";

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveForm4(Form4ViewModel model)
        {
            int studentId = ResolveStudentId(ParseNullableInt(model.StudentId));
            int advisorId = ResolveAdvisorId();

            await SaveCourseDecisionsAsync(studentId, model.PendingCourses);
            await SaveForm4ToDatabaseAsync(model, studentId, advisorId, "Draft", false);

            TempData["Success"] = "Form 4 saved successfully.";
            return RedirectToAction(nameof(Form4), new { studentId });
        }

        [HttpPost]
        public async Task<IActionResult> SendForm4(Form4ViewModel model)
        {
            int studentId = ResolveStudentId(ParseNullableInt(model.StudentId));
            int advisorId = ResolveAdvisorId();

            await SaveCourseDecisionsAsync(studentId, model.PendingCourses);
            await SaveForm4ToDatabaseAsync(model, studentId, advisorId, "Sent", true);

            TempData["Success"] = "Form 4 sent successfully.";
            return RedirectToAction(nameof(Form4), new { studentId });
        }

        [HttpPost]
        public async Task<IActionResult> ApproveFreeCourses(Form4ViewModel model)
        {
            int studentId = ResolveStudentId(ParseNullableInt(model.StudentId));

            await SaveCourseDecisionsAsync(studentId, model.PendingCourses);

            TempData["Success"] = "Free courses updated successfully.";
            return RedirectToAction(nameof(Form4), new { studentId });
        }

        private async Task SaveForm4ToDatabaseAsync(
            Form4ViewModel model,
            int studentId,
            int advisorId,
            string status,
            bool advisorConfirmation)
        {
            var student = await _context.Students
                .Include(s => s.Transcript)
                    .ThenInclude(t => t.Courses)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
                throw new InvalidOperationException("Student not found.");

            var vm = BuildForm4ViewModel(student);

            var form = new Db.Form
            {
                StudentId = studentId,
                AdvisorId = advisorId,
                FormType = "Form 4",
                FormDate = DateTime.Now,
                FormStatus = status,
                AdvisorNotes = model.AdvisorNotes,
                AutoFilled = true,
                AdvisorConfirmation = advisorConfirmation
            };

            _context.Forms.Add(form);
            await _context.SaveChangesAsync();

            var form4 = new Db.StudyPlanMatchingForm
            {
                FormId = form.FormId,
                GraduationStatus = null,
                RemainingHours = Math.Max(140 - vm.EarnedHours, 0),
                RequiredHours = 140,
                EarnedHours = vm.EarnedHours,
                RegisteredHours = null,
                UniversityHours = vm.UniversityReqHours,
                PrepYearHours = vm.PrepYearReqHours,
                FreeCoursesHours = vm.FreeCoursesHours,
                CollegeMandatoryHours = vm.CollegeMandatoryHours,
                DeptMandatoryHours = vm.DeptMandatoryHours,
                DeptElectiveHours = vm.DeptElectiveHours,
                TotalHours = vm.TotalHours
            };

            _context.StudyPlanMatchingForms.Add(form4);
            await _context.SaveChangesAsync();
        }

        private static int? ParseNullableInt(string? value)
        {
            return int.TryParse(value, out var result) ? result : null;
        }

        private void SaveForm4ToSession(Form4ViewModel model)
        {
            var json = JsonSerializer.Serialize(model);
            HttpContext.Session.SetString(Form4SessionKey, json);
        }

        private Form4ViewModel CreateNewForm4()
        {
            return new Form4ViewModel
            {
                StudentName = "Lama Alshikh",
                StudentId = "000000000",
                AcademicYear = DateTime.Now.Year.ToString(),
                EarnedHours = 129,
                RegisteredHours = 11,
                UniversityReqHours = 26,
                PrepYearReqHours = 15,
                FreeCoursesHours = 9,
                CollegeMandatoryHours = 24,
                DeptMandatoryHours = 57,
                DeptElectiveHours = 9,
                TotalHours = 140,
                GraduationTermText = "الفصل الدراسي الاول 2024",
                Note1 = "BUS220 و BUS320 تعادل إدارة المنظمات BUS 232",
                Note2 = "BUS230 و BUS335 تعادل إدارة المنظمات BUS 232",
                Note3 = "مادة التسويق تعادل BUS 232",
                Note4 = "تم احتساب 11 ساعة ضمن الفصل القادم",
                AdvisorNameLabel = "المرشدة الأكاديمية للطالبة",
                AdvisorName = "Dr. Amina Gamlo",
                AdvisorSignature = "",
                Status = "Draft"
            };
        }

        [HttpGet]
        public async Task<IActionResult> Form4History(int? studentId = null)
        {
            if (HttpContext.Session.GetString("UserRole") != "Advisor")
                return RedirectToAction("Login", "Account");

            int advisorId = ResolveAdvisorId();
            int resolvedStudentId = ResolveStudentId(studentId);

            var items = await _context.Forms
                .Where(f =>
                    f.StudentId == resolvedStudentId &&
                    f.AdvisorId == advisorId &&
                    f.FormType == "Form 4")
                .OrderByDescending(f => f.FormDate)
                .Select(f => new FormHistoryItemViewModel
                {
                    FormId = f.FormId,
                    FormTitle = "Study Plan Matching (Form 4)",
                    Status = f.FormStatus,
                    DateText = f.FormDate.ToString("MMM d, yyyy"),
                    ViewUrl = Url.Action("ViewSavedForm4", "Advisor", new { formId = f.FormId })!
                })
                .ToListAsync();

            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> ViewSavedForm4(int formId)
        {
            var form = await _context.Forms
                .Include(f => f.Student)
                    .ThenInclude(s => s.Transcript)
                        .ThenInclude(t => t.Courses)
                .Include(f => f.StudyPlanMatchingForm)
                .FirstOrDefaultAsync(f => f.FormId == formId && f.FormType == "Form 4");

            if (form == null || form.Student == null)
                return NotFound("Form 4 not found.");

            var model = BuildForm4ViewModel(form.Student);
            model.AdvisorNotes = form.AdvisorNotes ?? "";
            model.PlanCourseOptions = GetIsPlanCourseOptions();

            if (form.StudyPlanMatchingForm != null)
            {
                model.EarnedHours = form.StudyPlanMatchingForm.EarnedHours ?? model.EarnedHours;
                model.UniversityReqHours = form.StudyPlanMatchingForm.UniversityHours ?? model.UniversityReqHours;
                model.PrepYearReqHours = form.StudyPlanMatchingForm.PrepYearHours ?? model.PrepYearReqHours;
                model.FreeCoursesHours = form.StudyPlanMatchingForm.FreeCoursesHours ?? model.FreeCoursesHours;
                model.CollegeMandatoryHours = form.StudyPlanMatchingForm.CollegeMandatoryHours ?? model.CollegeMandatoryHours;
                model.DeptMandatoryHours = form.StudyPlanMatchingForm.DeptMandatoryHours ?? model.DeptMandatoryHours;
                model.DeptElectiveHours = form.StudyPlanMatchingForm.DeptElectiveHours ?? model.DeptElectiveHours;
                model.TotalHours = form.StudyPlanMatchingForm.TotalHours ?? model.TotalHours;
            }

            return View("Form4", model);
        }

        private string ExtractLastAcademicTerm(string? extractedInfo)
        {
            if (string.IsNullOrWhiteSpace(extractedInfo))
                return "غير محدد";

            var matches = Regex.Matches(
                extractedInfo,
                @"\b(FALL|SPRING|SUMMER|WINTER)\s+\d{4}/\d{4}\b",
                RegexOptions.IgnoreCase);

            if (matches.Count == 0)
                return "غير محدد";

            return matches[matches.Count - 1].Value.ToUpper();
        }

        private Form4ViewModel BuildForm4ViewModel(Db.Student student)
        {
            var transcript = student.Transcript;
            var courses = transcript?.Courses?.ToList() ?? new List<Db.Course>();

            var electiveCourses = new List<string>
            {
                "CPIS382","CPIS483","CPIS486","CPIS320",
                "CPIS420","CPIS424","CPIS360","CPIS363",
                "CPIS490","CPIS430","CPIS426","CPIS350"
            };

            int universityHours = 0;
            int prepYearHours = 0;
            int collegeMandatoryHours = 0;
            int deptMandatoryHours = 0;
            int deptElectiveHours = 0;
            int freeCoursesHours = 0;

            var pendingCourses = new List<Form4CourseDecisionItemVM>();

            var decisions = _context.TranscriptCourseDecisions
                .Where(d => d.StudentId == student.StudentId)
                .ToList();

            foreach (var course in courses)
            {
                if (course.RequirementCategory == "University")
                {
                    universityHours += course.Hours;
                    continue;
                }

                if (course.RequirementCategory == "PrepYear")
                {
                    prepYearHours += course.Hours;
                    continue;
                }

                if (course.RequirementCategory == "CollegeMandatory")
                {
                    collegeMandatoryHours += course.Hours;
                    continue;
                }

                if (course.RequirementCategory == "DeptMandatory")
                {
                    deptMandatoryHours += course.Hours;
                    continue;
                }

                if (electiveCourses.Contains(course.CourseId))
                {
                    deptElectiveHours += course.Hours;
                    continue;
                }

                var decision = decisions.FirstOrDefault(d => d.TranscriptCourseId == course.CourseId);

                if (decision != null)
                {
                    if (decision.DecisionType == "FreeElective")
                    {
                        freeCoursesHours += course.Hours;
                        continue;
                    }

                    if (decision.DecisionType == "EquivalentToPlan" &&
                        !string.IsNullOrWhiteSpace(decision.EquivalentCourseId))
                    {
                        var equivalentCourse = _context.Courses
                            .FirstOrDefault(c => c.CourseId == decision.EquivalentCourseId);

                        if (equivalentCourse != null)
                        {
                            if (equivalentCourse.RequirementCategory == "University")
                                universityHours += equivalentCourse.Hours;
                            else if (equivalentCourse.RequirementCategory == "PrepYear")
                                prepYearHours += equivalentCourse.Hours;
                            else if (equivalentCourse.RequirementCategory == "CollegeMandatory")
                                collegeMandatoryHours += equivalentCourse.Hours;
                            else if (equivalentCourse.RequirementCategory == "DeptMandatory")
                                deptMandatoryHours += equivalentCourse.Hours;
                            else if (electiveCourses.Contains(equivalentCourse.CourseId))
                                deptElectiveHours += equivalentCourse.Hours;
                            else
                                freeCoursesHours += equivalentCourse.Hours;

                            continue;
                        }
                    }
                }

                pendingCourses.Add(new Form4CourseDecisionItemVM
                {
                    TranscriptCourseId = course.CourseId,
                    TranscriptCourseName = course.CourseName,
                    Hours = course.Hours,
                    DecisionType = "",
                    EquivalentCourseId = null
                });
            }

            int categorizedHours = universityHours
                                 + prepYearHours
                                 + collegeMandatoryHours
                                 + deptMandatoryHours
                                 + deptElectiveHours
                                 + freeCoursesHours;

            int transcriptEarnedHours = ExtractTranscriptEarnedHours(transcript?.ExtractedInfo);

            if (transcriptEarnedHours <= 0)
                transcriptEarnedHours = categorizedHours;

            return new Form4ViewModel
            {
                StudentName = student.Name ?? "",
                StudentId = student.StudentId.ToString(),
                AcademicYear = DateTime.Now.Year.ToString(),
                EarnedHours = transcriptEarnedHours,
                UniversityReqHours = universityHours,
                PrepYearReqHours = prepYearHours,
                FreeCoursesHours = freeCoursesHours,
                CollegeMandatoryHours = collegeMandatoryHours,
                DeptMandatoryHours = deptMandatoryHours,
                DeptElectiveHours = deptElectiveHours,
                TotalHours = transcriptEarnedHours,
                GraduationTermText = ExtractLastAcademicTerm(transcript?.ExtractedInfo),
                AdvisorNameLabel = "المرشدة الأكاديمية للطالبة",
                AdvisorName = "",
                AdvisorNotes = "",
                PendingCourses = pendingCourses
            };
        }

        private int ExtractFreeCourseHoursFromTranscriptPdf(Db.Transcript? transcript, List<string> electiveCourses)
        {
            if (transcript == null || string.IsNullOrWhiteSpace(transcript.PdfFile))
                return 0;

            var freeCourseIds = transcript.Courses?
                .Where(c =>
                    c.RequirementCategory != "University" &&
                    c.RequirementCategory != "PrepYear" &&
                    c.RequirementCategory != "CollegeMandatory" &&
                    c.RequirementCategory != "DeptMandatory" &&
                    !electiveCourses.Contains(c.CourseId))
                .Select(c => c.CourseId)
                .Distinct()
                .ToList() ?? new List<string>();

            if (freeCourseIds.Count == 0)
                return 0;

            var cleanRelativePath = transcript.PdfFile.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_env.WebRootPath, cleanRelativePath);

            if (!System.IO.File.Exists(fullPath))
                return 0;

            var hourMap = ExtractFreeCourseHoursMapFromPdf(fullPath, freeCourseIds);

            int total = 0;
            foreach (var id in freeCourseIds)
            {
                if (hourMap.TryGetValue(id, out var h))
                    total += h;
            }

            return total;
        }

        private Dictionary<string, int> ExtractFreeCourseHoursMapFromPdf(string fullPath, List<string> freeCourseIds)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            using var doc = PdfDocument.Open(fullPath);

            foreach (var page in doc.GetPages())
            {
                var words = page.GetWords()
                    .OrderByDescending(w => w.BoundingBox.Bottom)
                    .ThenBy(w => w.BoundingBox.Left)
                    .ToList();

                var lines = GroupWordsIntoLines(words);

                foreach (var line in lines)
                {
                    var tokens = line
                        .OrderBy(w => w.BoundingBox.Left)
                        .Select(w => w.Text.Trim().ToUpperInvariant())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .ToList();

                    if (tokens.Count == 0)
                        continue;

                    foreach (var courseId in freeCourseIds)
                    {
                        if (result.ContainsKey(courseId))
                            continue;

                        if (!TryFindCourseOnLine(tokens, courseId, out var codeIndex))
                            continue;

                        if (TryExtractHourNearCourse(tokens, codeIndex, out var hours))
                            result[courseId] = hours;
                    }
                }
            }

            return result;
        }

        private List<List<UglyToad.PdfPig.Content.Word>> GroupWordsIntoLines(List<UglyToad.PdfPig.Content.Word> words)
        {
            var lines = new List<List<UglyToad.PdfPig.Content.Word>>();

            foreach (var word in words)
            {
                var existingLine = lines.FirstOrDefault(line =>
                    Math.Abs(line[0].BoundingBox.Bottom - word.BoundingBox.Bottom) <= 3.5);

                if (existingLine == null)
                    lines.Add(new List<UglyToad.PdfPig.Content.Word> { word });
                else
                    existingLine.Add(word);
            }

            return lines;
        }

        private bool TryFindCourseOnLine(List<string> tokens, string courseId, out int codeIndex)
        {
            codeIndex = -1;

            var upperCourseId = courseId.ToUpperInvariant();
            var prefix = new string(upperCourseId.TakeWhile(char.IsLetter).ToArray());
            var number = new string(upperCourseId.SkipWhile(char.IsLetter).ToArray());

            for (int i = 0; i < tokens.Count; i++)
            {
                var m = Regex.Match(tokens[i], @"^([A-Z]{3,6})[-]?(\d{3})$");
                if (m.Success)
                {
                    var code = (m.Groups[1].Value + m.Groups[2].Value).ToUpperInvariant();
                    if (code == upperCourseId)
                    {
                        codeIndex = i;
                        return true;
                    }
                }

                if (i < tokens.Count - 1 &&
                    Regex.IsMatch(tokens[i], @"^[A-Z]{3,6}$") &&
                    tokens[i] == prefix &&
                    tokens[i + 1] == number)
                {
                    codeIndex = i;
                    return true;
                }
            }

            return false;
        }

        private bool TryExtractHourNearCourse(List<string> tokens, int codeIndex, out int hours)
        {
            hours = 0;

            var candidates = new List<(int Distance, int Value, bool IsBefore)>();

            for (int i = Math.Max(0, codeIndex - 10); i <= Math.Min(tokens.Count - 1, codeIndex + 10); i++)
            {
                if (i == codeIndex)
                    continue;

                if (Regex.IsMatch(tokens[i], @"^[0-5]$"))
                {
                    int value = int.Parse(tokens[i]);
                    int distance = Math.Abs(i - codeIndex);
                    bool isBefore = i < codeIndex;

                    candidates.Add((distance, value, isBefore));
                }
            }

            if (candidates.Count == 0)
                return false;

            var best = candidates
                .OrderBy(c => c.Distance)
                .ThenBy(c => c.IsBefore ? 0 : 1)
                .ThenBy(c => c.Value == 0 ? 1 : 0)
                .First();

            hours = best.Value;
            return true;
        }

        private int ExtractTranscriptEarnedHours(string? extractedInfo)
        {
            if (string.IsNullOrWhiteSpace(extractedInfo))
                return 0;

            var normalized = Regex.Replace(extractedInfo, @"\s+", " ").Trim();

            var grandTotalMatch = Regex.Match(
                normalized,
                @"(\d+)\s*Grand\s*Total",
                RegexOptions.IgnoreCase);

            if (grandTotalMatch.Success && int.TryParse(grandTotalMatch.Groups[1].Value, out var grandTotal))
                return grandTotal;

            var cumulativeTotalMatch = Regex.Match(
                normalized,
                @"(\d+)\s*Cumulative\s*Total",
                RegexOptions.IgnoreCase);

            if (cumulativeTotalMatch.Success && int.TryParse(cumulativeTotalMatch.Groups[1].Value, out var cumulativeTotal))
                return cumulativeTotal;

            return 0;
        }
        private List<PlanCourseOptionVM> GetIsPlanCourseOptions()
        {
            return (
                from spc in _context.StudyPlanCourses
                join p in _context.StudyPlans on spc.PlanId equals p.PlanId
                join c in _context.Courses on spc.CourseId equals c.CourseId
                where p.Major != null &&
                      p.Major.Trim().ToUpper() == "INFORMATION SYSTEMS"
                group c by new { c.CourseId, c.CourseName } into g
                orderby g.Key.CourseId
                select new PlanCourseOptionVM
                {
                    CourseId = g.Key.CourseId,
                    CourseName = g.Key.CourseName
                }
            ).ToList();
        }

        private bool IsDirectlyClassifiedCourse(Db.Course course, List<string> electiveCourses)
        {
            return course.RequirementCategory == "University"
                || course.RequirementCategory == "PrepYear"
                || course.RequirementCategory == "CollegeMandatory"
                || course.RequirementCategory == "DeptMandatory"
                || electiveCourses.Contains(course.CourseId);
        }

        private async Task SaveCourseDecisionsAsync(int studentId, List<Form4CourseDecisionItemVM> pendingCourses)
        {
            if (pendingCourses == null || pendingCourses.Count == 0)
                return;

            foreach (var item in pendingCourses)
            {
                if (string.IsNullOrWhiteSpace(item.TranscriptCourseId))
                    continue;

                if (string.IsNullOrWhiteSpace(item.DecisionType))
                    continue;

                var existing = await _context.TranscriptCourseDecisions
                    .FirstOrDefaultAsync(x =>
                        x.StudentId == studentId &&
                        x.TranscriptCourseId == item.TranscriptCourseId);

                if (existing == null)
                {
                    existing = new Db.TranscriptCourseDecision
                    {
                        StudentId = studentId,
                        TranscriptCourseId = item.TranscriptCourseId
                    };

                    _context.TranscriptCourseDecisions.Add(existing);
                }

                existing.DecisionType = item.DecisionType;

                existing.EquivalentCourseId = item.DecisionType == "EquivalentToPlan"
                    ? item.EquivalentCourseId
                    : null;

                existing.IsApprovedByAdvisor = true;
            }

            await _context.SaveChangesAsync();
        }

        /* ========================================================
                            Chat + Form 3 Auto Fill
           ======================================================== */

        // =======================
        // Advisor Chat
        // =======================

        private async Task<(Db.Meeting meeting, Db.Student student, string advisorName)?> GetOrCreateAdvisorMeetingAsync()
        {
            int? advisorId = HttpContext.Session.GetInt32("AdvisorId");

            if (!advisorId.HasValue)
                return null;

            var advisor = await _context.Advisors
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.AdvisorId == advisorId.Value);

            if (advisor == null)
                return null;

            int? selectedStudentId = HttpContext.Session.GetInt32("AdvisorChatStudentId");

            var studentQuery = _context.Students
                .Where(s => s.AdvisorId == advisor.AdvisorId);

            if (selectedStudentId.HasValue)
            {
                studentQuery = studentQuery.Where(s => s.StudentId == selectedStudentId.Value);
            }

            var student = await studentQuery
                .OrderBy(s => s.StudentId)
                .FirstOrDefaultAsync();

            if (student == null)
                return null;

            // هنا الجديد: نحاول نستخدم الاجتماع المحدد في السيشن أولًا
            int? selectedMeetingId = HttpContext.Session.GetInt32("AdvisorChatMeetingId");

            Db.Meeting? meeting = null;

            if (selectedMeetingId.HasValue)
            {
                meeting = await _context.Meetings
                    .FirstOrDefaultAsync(m =>
                        m.MeetingId == selectedMeetingId.Value &&
                        m.StudentId == student.StudentId &&
                        m.AdvisorId == advisor.AdvisorId);
            }

            // إذا ما لقينا اجتماع محدد، نجيب آخر اجتماع للطالبة
            if (meeting == null)
            {
                meeting = await _context.Meetings
                    .Where(m => m.StudentId == student.StudentId && m.AdvisorId == advisor.AdvisorId)
                    .OrderByDescending(m => m.MeetingId)
                    .FirstOrDefaultAsync();
            }

            // إذا ما فيه أي اجتماع، ننشئ واحد جديد
            if (meeting == null)
            {
                meeting = new Db.Meeting
                {
                    StudentId = student.StudentId,
                    AdvisorId = advisor.AdvisorId,
                    StartTime = DateTime.Now,
                    IsRecordingStarted = false,
                    LastRecordingAction = null
                };

                _context.Meetings.Add(meeting);
                await _context.SaveChangesAsync();
            }

            // مهم: نحفظ meetingId الحالي في السيشن عشان Start/Stop/SendMessage كلهم يستخدمون نفس الاجتماع
            HttpContext.Session.SetInt32("AdvisorChatMeetingId", meeting.MeetingId);

            string advisorName = advisor.User?.Name ?? "Advisor";

            return (meeting, student, advisorName);
        }

        [HttpGet]
        public async Task<IActionResult> Chat()
        {
            if (HttpContext.Session.GetString("UserRole") != "Advisor")
                return RedirectToAction("Login", "Account");

            var data = await GetOrCreateAdvisorMeetingAsync();

            if (data == null)
                return NotFound("No assigned student or meeting was found.");

            var meeting = data.Value.meeting;
            var student = data.Value.student;
            var advisorName = data.Value.advisorName;

            string recordingMessage = "";

            if (meeting.LastRecordingAction == "started")
                recordingMessage = "The chat recording is started";
            else if (meeting.LastRecordingAction == "stopped")
                recordingMessage = "The chat recording is stopped";

            ViewBag.RecordingMessage = recordingMessage;

            var studentNameLower = student.Name.Trim().ToLower();

            var messages = await _context.MeetingMessages
                .Where(m => m.MeetingId == meeting.MeetingId)
                .OrderBy(m => m.MessageDate)
                .Select(m => new ChatMessageVM
                {
                    SenderName = (m.SenderName ?? "")
                        .Replace("(me)", "", StringComparison.OrdinalIgnoreCase)
                        .Trim(),

                    Text = m.MessageText,

                    IsFromStudent = m.SenderName != null &&
                                    m.SenderName.Trim().ToLower().Contains(studentNameLower),

                    TimeText = m.MessageDate.HasValue
                        ? m.MessageDate.Value.ToString("hh:mm tt")
                        : "",

                    IsRecorded = m.IsRecorded
                })
                .ToListAsync();
            var lastMessage = messages.LastOrDefault();

            // يظهر للمرشد إذا آخر رسالة من الطالبة
            ViewBag.HasNewStudentMessage = lastMessage != null && lastMessage.IsFromStudent;

            var model = new StudentChatViewModel
            {
                AdvisorName = advisorName,
                StudentName = student.Name,
                IsRecordingStarted = meeting.IsRecordingStarted,
                Messages = messages
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> StartRecording()
        {
            if (HttpContext.Session.GetString("UserRole") != "Advisor")
                return RedirectToAction("Login", "Account");

            var data = await GetOrCreateAdvisorMeetingAsync();

            if (data == null)
                return NotFound("No assigned student or meeting was found.");

            var meeting = data.Value.meeting;

            meeting.IsRecordingStarted = true;
            meeting.LastRecordingAction = "started";
            meeting.RecordingStartedAt = DateTime.Now;
            meeting.RecordingStoppedAt = null;
            meeting.ChatRecord = null;
            meeting.ChatSummary = null;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Chat));
        }

        [HttpPost]
        public async Task<IActionResult> StopRecording()
        {
            if (HttpContext.Session.GetString("UserRole") != "Advisor")
                return RedirectToAction("Login", "Account");
            var data = await GetOrCreateAdvisorMeetingAsync();

            if (data == null)
                return NotFound("No assigned student or meeting was found.");

            var meeting = data.Value.meeting;
            var student = data.Value.student;

            meeting.IsRecordingStarted = false;
            meeting.LastRecordingAction = "stopped";
            meeting.RecordingStoppedAt = DateTime.Now;

            if (meeting.RecordingStartedAt.HasValue && meeting.RecordingStoppedAt.HasValue)
            {
                var start = meeting.RecordingStartedAt.Value;
                var stop = meeting.RecordingStoppedAt.Value;

                var recordedMessages = await _context.MeetingMessages
                    .Where(m => m.MeetingId == meeting.MeetingId
                                && m.MessageDate.HasValue
                                && m.MessageDate.Value >= start
                                && m.MessageDate.Value <= stop)
                    .OrderBy(m => m.MessageDate)
                    .ToListAsync();

                if (recordedMessages.Any())
                {
                    string studentNameLower = student.Name.Trim().ToLower();

                    var lines = recordedMessages.Select(m =>
                    {
                        var sender = (m.SenderName ?? "").Trim().ToLower().Contains(studentNameLower)
                            ? "Student"
                            : "Advisor";

                        return $"{sender}: {m.MessageText}";
                    });

                    meeting.ChatRecord = string.Join(Environment.NewLine, lines);

                    if (!string.IsNullOrWhiteSpace(meeting.ChatRecord))
                    {
                        try
                        {
                            var summary = await _aiSummaryService.SummarizeMeetingChatAsync(meeting.ChatRecord);
                            meeting.ChatSummary = summary;
                        }
                        catch
                        {
                            meeting.ChatSummary = null;
                        }
                    }
                }
                else
                {
                    meeting.ChatRecord = null;
                    meeting.ChatSummary = null;
                }
            }
            else
            {
                meeting.ChatRecord = null;
                meeting.ChatSummary = null;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Chat));
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(string message)
        {
            if (HttpContext.Session.GetString("UserRole") != "Advisor")
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(message))
                return RedirectToAction(nameof(Chat));

            var data = await GetOrCreateAdvisorMeetingAsync();

            if (data == null)
                return NotFound("No assigned student or meeting was found.");

            var meeting = data.Value.meeting;
            var advisorName = data.Value.advisorName;

            var newMessage = new Db.MeetingMessage
            {
                MeetingId = meeting.MeetingId,
                SenderName = advisorName + " (me)",
                MessageText = message.Trim(),
                MessageDate = DateTime.Now,
                IsRecorded = meeting.IsRecordingStarted
            };

            _context.MeetingMessages.Add(newMessage);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Chat));
        }

        [HttpPost]
        public async Task<IActionResult> BuildChatRecord()
        {
            var data = await GetOrCreateAdvisorMeetingAsync();

            if (data == null)
                return NotFound("No assigned student or meeting was found.");

            var meeting = data.Value.meeting;
            var student = data.Value.student;

            if (!meeting.RecordingStartedAt.HasValue || !meeting.RecordingStoppedAt.HasValue)
            {
                meeting.ChatRecord = null;
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Chat));
            }

            var start = meeting.RecordingStartedAt.Value;
            var stop = meeting.RecordingStoppedAt.Value;

            var recordedMessages = await _context.MeetingMessages
                .Where(m => m.MeetingId == meeting.MeetingId
                            && m.MessageDate.HasValue
                            && m.MessageDate.Value >= start
                            && m.MessageDate.Value <= stop)
                .OrderBy(m => m.MessageDate)
                .ToListAsync();
            if (!recordedMessages.Any())
            {
                meeting.ChatRecord = null;
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Chat));
            }

            string studentNameLower = student.Name.Trim().ToLower();

            var lines = recordedMessages.Select(m =>
            {
                var sender = (m.SenderName ?? "").Trim().ToLower().Contains(studentNameLower)
                    ? "Student"
                    : "Advisor";

                return $"{sender}: {m.MessageText}";
            });

            meeting.ChatRecord = string.Join(Environment.NewLine, lines);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Chat));
        }

        [HttpPost]
        public async Task<IActionResult> GenerateChatSummary()
        {
            var data = await GetOrCreateAdvisorMeetingAsync();

            if (data == null)
                return NotFound("No assigned student or meeting was found.");

            var meeting = data.Value.meeting;

            if (string.IsNullOrWhiteSpace(meeting.ChatRecord))
            {
                TempData["Success"] = "لا يوجد ChatRecord لتلخيصه.";
                return RedirectToAction(nameof(Form3));
            }

            try
            {
                var summary = await _aiSummaryService.SummarizeMeetingChatAsync(meeting.ChatRecord);

                meeting.ChatSummary = summary;
                await _context.SaveChangesAsync();

                TempData["Success"] = "تم توليد الملخص الذكي بنجاح.";
            }
            catch (Exception ex)
            {
                TempData["Success"] = "حدث خطأ أثناء التلخيص: " + ex.Message;
            }

            return RedirectToAction(nameof(Form3));
        }
        [HttpPost("/Advisor/GenerateSuggestedSchedule")]
        public async Task<IActionResult> GenerateSuggestedSchedule(int studentId)
        {
            var student = await _context.Students
                .Include(s => s.Transcript)
                    .ThenInclude(t => t.Courses)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
            {
                TempData["Error"] = "Student was not found.";
                return RedirectToAction(nameof(Form3));
            }

            var completedCourses = student.Transcript?.Courses?
                .Select(c => $"{c.CourseId} - {c.CourseName} ({c.Hours} hours)")
                .ToList() ?? new List<string>();

            var transcriptText = string.Join(", ", completedCourses);

            // استبدل استعلام availableCoursesList بهذا الكود المؤقت للتجربة
            // التعديل الصحيح بناءً على أسماء جداولك الحقيقية
            var availableCoursesList = await (
    from spc in _context.StudyPlanCourses // تأكد أنها بالجمع كما في ملف الـ Context
    join p in _context.StudyPlans on spc.PlanId equals p.PlanId // بالجمع
    join c in _context.Courses on spc.CourseId equals c.CourseId // بالجمع
    where p.Major != null && p.Major.Trim().ToUpper() == "INFORMATION SYSTEMS"
    select c.CourseId + " - " + c.CourseName
).Distinct().ToListAsync();

            var coursesText = string.Join(", ", availableCoursesList);

            if (string.IsNullOrWhiteSpace(transcriptText))
            {
                TempData["Error"] = "There is no academic transcript data for this student.";
                return RedirectToAction(nameof(Form3), new { studentId });
            }

            if (string.IsNullOrWhiteSpace(coursesText))
            {
                TempData["Error"] = "There are no available study plan courses.";
                return RedirectToAction(nameof(Form3), new { studentId });
            }

            try
            {
                var suggestion = await _aiSummaryService.SuggestScheduleAsync(transcriptText, coursesText);

                TempData["AiSuggestion"] = suggestion;
                TempData["Success"] = "The suggested schedule was generated successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while generating the suggested schedule: " + ex.Message;
            }

            return RedirectToAction(nameof(Form3), new { studentId });
        }
    }
    // أضف هذا الكلاس في أسفل الملف لكي يتعرف عليه الكنترولر
    public class SendAdvisorMessageRequest
    {
        public string Message { get; set; }
    }
}
