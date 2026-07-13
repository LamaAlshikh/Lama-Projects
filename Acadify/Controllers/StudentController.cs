using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Acadify.Models;
using Db = Acadify.Models.Db;
using Acadify.Models.StudentPages;
using Acadify.Services;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using UglyToad.PdfPig;

namespace Acadify.Controllers
{
    public class StudentController : Controller
    {
        private readonly Db.AcadifyDbContext _db; private readonly IWebHostEnvironment _env; private readonly ITranscriptParserService _transcriptParserService; private readonly IRecommendationEngineService _recommendationEngineService; private readonly ITranscriptAiParserService _transcriptAiParserService; private readonly IConfiguration _configuration;

        private const int GraduationRequiredHours = 140;

        public StudentController(
       Db.AcadifyDbContext db,
       IWebHostEnvironment env,
       ITranscriptParserService transcriptParserService,
       IRecommendationEngineService recommendationEngineService,
       ITranscriptAiParserService transcriptAiParserService,
       IConfiguration configuration)
        {
            _db = db;
            _env = env;
            _transcriptParserService = transcriptParserService;
            _recommendationEngineService = recommendationEngineService;
            _transcriptAiParserService = transcriptAiParserService;
            _configuration = configuration;
        }
        // =========================
        // Notification helpers
        // =========================
        private async Task AddNotificationAsync(
            string senderRole,
            string sourceType,
            string type,
            string message,
            int? studentId = null,
            int? advisorId = null,
            int? adminId = null,
            bool sendEmail = false)
        {
            var notification = new Db.Notification
            {
                SenderRole = senderRole,
                SourceType = sourceType,
                Type = type,
                Message = message,
                StudentId = studentId,
                AdvisorId = advisorId,
                AdminId = adminId,
                Date = DateTime.Now,
                IsRead = false
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();

            // Email is sent only when sendEmail is true.
            // In our system, this should be used for important academic dates only.
            if (!sendEmail)
                return;

            var recipientEmails = await GetRecipientEmailsAsync(studentId, advisorId, adminId);

            foreach (var email in recipientEmails)
            {
                await SendNotificationEmailAsync(
                    email,
                    $"Acadify Notification - {BuildEmailTitle(sourceType)}",
                    message);
            }
        }

        private async Task AddNotificationToAllAdminsAsync(
            string senderRole,
            string sourceType,
            string type,
            string message,
            int? studentId = null,
            int? advisorId = null,
            bool sendEmail = false)
        {
            var admins = await _db.Admins
                .Include(a => a.User)
                .ToListAsync();

            var adminEmails = new List<string>();

            foreach (var admin in admins)
            {
                var notification = new Db.Notification
                {
                    SenderRole = senderRole,
                    SourceType = sourceType,
                    Type = type,
                    Message = message,
                    StudentId = studentId,
                    AdvisorId = advisorId,
                    AdminId = admin.AdminId,
                    Date = DateTime.Now,
                    IsRead = false
                };

                _db.Notifications.Add(notification);

                if (sendEmail && !string.IsNullOrWhiteSpace(admin.User?.Email))
                    adminEmails.Add(admin.User.Email);
            }

            await _db.SaveChangesAsync();

            // Email is sent only when sendEmail is true.
            if (!sendEmail)
                return;

            foreach (var email in adminEmails.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                await SendNotificationEmailAsync(
                    email,
                    $"Acadify Notification - {BuildEmailTitle(sourceType)}",
                    message);
            }
        }

        private async Task AddRecommendationNotificationToAdvisorAsync(
            int studentId,
            string type,
            string message)
        {
            var student = await _db.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null || !student.AdvisorId.HasValue)
                return;

            var notification = new Db.Notification
            {
                SenderRole = "Student",
                SourceType = "Recommendation",

                // Student id is saved inside Type so the advisor notification can open Form 2.
                Type = $"{type}-student-{student.StudentId}",

                Message = $"{student.Name}: {message}",

                // This notification is for the advisor only.
                StudentId = null,
                AdvisorId = student.AdvisorId.Value,
                AdminId = null,

                Date = DateTime.Now,
                IsRead = false
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();
        }

        private async Task<List<string>> GetRecipientEmailsAsync(
            int? studentId,
            int? advisorId,
            int? adminId)
        {
            var emails = new List<string>();

            if (studentId.HasValue)
            {
                var studentEmail = await _db.Students
                    .Where(s => s.StudentId == studentId.Value)
                    .Select(s => s.User.Email)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrWhiteSpace(studentEmail))
                    emails.Add(studentEmail);
            }

            if (advisorId.HasValue)
            {
                var advisorEmail = await _db.Advisors
                    .Where(a => a.AdvisorId == advisorId.Value)
                    .Select(a => a.User.Email)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrWhiteSpace(advisorEmail))
                    emails.Add(advisorEmail);
            }

            if (adminId.HasValue)
            {
                var adminEmail = await _db.Admins
                    .Where(a => a.AdminId == adminId.Value)
                    .Select(a => a.User.Email)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrWhiteSpace(adminEmail))
                    emails.Add(adminEmail);
            }

            return emails
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private async Task SendNotificationEmailAsync(string? toEmail, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                return;

            var host = _configuration["Smtp:Host"];
            var username = _configuration["Smtp:Username"];
            var password = _configuration["Smtp:Password"];
            var fromEmail = _configuration["Smtp:FromEmail"] ?? username;

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(fromEmail))
            {
                return;
            }

            int port = 587;
            int.TryParse(_configuration["Smtp:Port"], out port);

            bool enableSsl = true;
            bool.TryParse(_configuration["Smtp:EnableSsl"], out enableSsl);

            try
            {
                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = enableSsl,
                    Credentials = new NetworkCredential(username, password)
                };

                using var mail = new MailMessage(fromEmail, toEmail)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false
                };

                await client.SendMailAsync(mail);
            }
            catch
            {
                // Email failure should not stop the main system action.
            }
        }

        private static string BuildEmailTitle(string? sourceType)
        {
            return sourceType switch
            {
                "Request" => "Student Request",
                "StudentAssigned" => "Student Assigned",
                "Calendar" => "Academic Calendar",
                "Transcript" => "Transcript",
                "Recommendation" => "Course Recommendation",
                "Form" => "Form Notification",
                "Meeting" => "Meeting Notification",
                _ => "Notification"
            };
        }

        private async Task AddGeneratedFormsNotificationsAsync(Db.Student student)
        {
            var messages = new List<string>
    {
        "Academic Advising Confirmation form is generated from the transcript.",
        "Next Semester Course Selection form is generated from the transcript.",
        "Meeting Record Form is generated from the transcript.",
        "Study Plan Matching form is generated from the transcript.",
        "Graduation Project Eligibility form is generated from the transcript."
    };

            foreach (var msg in messages)
            {
                if (student.AdvisorId.HasValue)
                {
                    await AddNotificationAsync(
                        senderRole: "System",
                        sourceType: "Form",
                        type: "generated form",
                        message: $"{student.Name}: {msg}",
                        studentId: student.StudentId,
                        advisorId: student.AdvisorId.Value,
                        sendEmail: false);
                }
                else
                {
                    await AddNotificationToAllAdminsAsync(
                        senderRole: "System",
                        sourceType: "Form",
                        type: "generated form",
                        message: $"{student.Name}: {msg}",
                        studentId: student.StudentId,
                        sendEmail: false);
                }
            }
        }
        // =========================
        // General helpers
        // =========================
        private int? GetCurrentStudentId()
        {
            return HttpContext.Session.GetInt32("StudentId");
        }

        private async Task<int?> GetAdvisorIdForStudentAsync(int studentId)
        {
            return await _db.Students
                .Where(s => s.StudentId == studentId)
                .Select(s => (int?)s.AdvisorId)
                .FirstOrDefaultAsync();
        }

        private async Task<bool> HasUploadedTranscriptAsync(int studentId)
        {
            return await _db.Transcripts
                .AnyAsync(t => t.StudentId == studentId &&
                               !string.IsNullOrWhiteSpace(t.PdfFile));
        }

        private async Task<IActionResult?> RedirectIfTranscriptMissingAsync(int studentId)
        {
            bool hasTranscript = await HasUploadedTranscriptAsync(studentId);

            if (!hasTranscript)
            {
                TempData["TranscriptRequired"] = "Please upload your transcript first.";
                return RedirectToAction(nameof(UploadTranscript));
            }

            return null;
        }

        private static string NormalizeCalendarText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value
                .Trim()
                .ToLowerInvariant()
                .Replace("أ", "ا")
                .Replace("إ", "ا")
                .Replace("آ", "ا")
                .Replace("ة", "ه")
                .Replace("ى", "ي")
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("_", "");
        }

        private static bool IsStudentRegistrationEvent(string? eventName, bool isStart)
        {
            var text = NormalizeCalendarText(eventName);

            bool hasStartOrEnd = isStart
                ? text.Contains("بدايه") || text.Contains("start") || text.Contains("beginning")
                : text.Contains("نهايه") || text.Contains("end");

            bool hasRegistration =
                text.Contains("تسجيلالمقررات") ||
                text.Contains("registration");

            bool hasStudent =
                text.Contains("للطالب") ||
                text.Contains("للطلاب") ||
                text.Contains("students");

            return hasStartOrEnd && hasRegistration && hasStudent;
        }

        private async Task<(bool IsActive, DateTime StartDate, DateTime EndDate)> GetActiveTranscriptUploadPeriodAsync()
        {
            var latestCalendarId = await _db.AcademicCalendars
                .OrderByDescending(c => c.UploadedAt)
                .Select(c => (int?)c.CalendarId)
                .FirstOrDefaultAsync();

            if (!latestCalendarId.HasValue)
                return (false, DateTime.MinValue, DateTime.MinValue);

            var events = await _db.AcademicCalendarEvents
                .Where(e => e.CalendarId == latestCalendarId.Value)
                .ToListAsync();

            var startEvent = events
                .Where(e => IsStudentRegistrationEvent(e.EventName, true))
                .OrderBy(e => e.GregorianDate)
                .FirstOrDefault();

            var endEvent = events
                .Where(e => IsStudentRegistrationEvent(e.EventName, false))
                .OrderByDescending(e => e.GregorianDate)
                .FirstOrDefault();

            if (startEvent == null || endEvent == null)
                return (false, DateTime.MinValue, DateTime.MinValue);

            var today = DateTime.Today;
            var startDate = startEvent.GregorianDate.Date;
            var endDate = endEvent.GregorianDate.Date;

            bool isActive = today >= startDate && today <= endDate;

            return (isActive, startDate, endDate);
        }

        private async Task<IActionResult?> RedirectIfTranscriptUploadRequiredForCurrentSemesterAsync(int studentId)
        {
            var period = await GetActiveTranscriptUploadPeriodAsync();

            if (!period.IsActive)
                return null;

            bool hasUploadedForThisPeriod = await _db.Transcripts
                .AnyAsync(t =>
                    t.StudentId == studentId &&
                    !string.IsNullOrWhiteSpace(t.PdfFile) &&
                    t.UploadedAt.HasValue &&
                    t.UploadedAt.Value.Date >= period.StartDate.Date);

            if (!hasUploadedForThisPeriod)
            {
                TempData["TranscriptRequired"] = "Please upload your transcript for the current semester.";
                return RedirectToAction(nameof(UploadTranscript));
            }

            return null;
        }

        private async Task LoadStudentSidebarDataAsync()
        {
            int? studentId = GetCurrentStudentId();

            if (!studentId.HasValue)
            {
                ViewBag.StudentName = "Student";
                ViewBag.StudentEmail = HttpContext.Session.GetString("UserEmail") ?? "";
                return;
            }

            var student = await _db.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId.Value);

            if (student == null)
            {
                ViewBag.StudentName = "Student";
                ViewBag.StudentEmail = HttpContext.Session.GetString("UserEmail") ?? "";
                return;
            }

            ViewBag.StudentName = GetStringPropertyValue(student, "Name", "StudentName", "FullName");
            ViewBag.StudentEmail = HttpContext.Session.GetString("UserEmail")
                ?? GetStringPropertyValue(student, "Email", "StudentEmail", "UniversityEmail");
        }

        private static string GetStringPropertyValue(object obj, params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                var prop = obj.GetType().GetProperty(propertyName);
                if (prop != null)
                {
                    var value = prop.GetValue(obj)?.ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }

            return string.Empty;
        }

        // =========================
        // Transcript PDF helpers
        // =========================

        private static bool IsRealPdfFile(IFormFile file)
        {
            if (file == null || file.Length < 5)
                return false;

            using var stream = file.OpenReadStream();

            byte[] header = new byte[5];
            int bytesRead = stream.Read(header, 0, header.Length);

            if (bytesRead < 5)
                return false;

            string fileHeader = Encoding.ASCII.GetString(header);
            return fileHeader == "%PDF-";
        }

        private static void DeleteFileIfExists(string fullPath)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(fullPath) && System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
            }
            catch
            {
                // Ignore delete errors.
            }
        }

        private static bool LooksLikeTranscript(
            string? extractedText,
            IReadOnlyCollection<string> courseCodes,
            decimal? cumulativeGpa,
            decimal? latestTermGpa)
        {
            if (string.IsNullOrWhiteSpace(extractedText))
                return false;

            var normalized = Regex.Replace(extractedText, @"\s+", " ").ToUpperInvariant();

            bool hasReadableText = normalized.Length >= 100;

            bool hasTranscriptKeyword =
                normalized.Contains("TRANSCRIPT") ||
                normalized.Contains("ACADEMIC RECORD") ||
                normalized.Contains("ACADEMIC TRANSCRIPT") ||
                normalized.Contains("سجل أكاديمي") ||
                normalized.Contains("السجل الأكاديمي");

            bool hasStudentKeyword =
                normalized.Contains("STUDENT") ||
                normalized.Contains("STUDENT ID") ||
                normalized.Contains("ID") ||
                normalized.Contains("UNIVERSITY") ||
                normalized.Contains("KING ABDULAZIZ") ||
                normalized.Contains("جامعة");

            bool hasGpaEvidence =
                cumulativeGpa.HasValue ||
                latestTermGpa.HasValue ||
                normalized.Contains("GPA") ||
                normalized.Contains("CUMULATIVE");

            bool hasCourseEvidence =
                courseCodes != null &&
                courseCodes.Count >= 2;

            bool hasGradeEvidence =
                normalized.Contains("GRADE") ||
                normalized.Contains("GRADES") ||
                normalized.Contains("PASSED") ||
                normalized.Contains("CREDIT") ||
                normalized.Contains("CREDIT HOURS") ||
                normalized.Contains("HOURS");

            return hasReadableText &&
                   (
                       (hasTranscriptKeyword && (hasGpaEvidence || hasCourseEvidence)) ||
                       (hasGpaEvidence && hasCourseEvidence) ||
                       (hasStudentKeyword && hasCourseEvidence && hasGradeEvidence)
                   );
        }





        private static string ReadPdfText(string fullPath)
        {
            var sb = new StringBuilder();

            using (var document = PdfDocument.Open(fullPath))
            {
                foreach (var page in document.GetPages())
                    sb.AppendLine(page.Text);
            }

            return sb.ToString();
        }

        private sealed class ParsedTranscript
        {
            public decimal? CumulativeGpa { get; set; }
            public decimal? LatestTermGpa { get; set; }
            public HashSet<string> CourseCodes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private static ParsedTranscript ParseTranscriptText(string text)
        {
            var result = new ParsedTranscript();
            if (string.IsNullOrWhiteSpace(text))
                return result;

            var normalized = Regex.Replace(text, @"\s+", " ").Trim();

            var cumMatch = Regex.Match(
                normalized,
                @"Cumulative\s*GPA\s*[:\-]?\s*([0-5]\.\d{2})",
                RegexOptions.IgnoreCase);

            if (!cumMatch.Success)
            {
                cumMatch = Regex.Match(
                    normalized,
                    @"([0-5]\.\d{2})\s*Cumulative\s*GPA",
                    RegexOptions.IgnoreCase);
            }

            if (!cumMatch.Success)
            {
                cumMatch = Regex.Match(
                    normalized,
                    @"\b\d+\s+([0-5]\.\d{2})\s*Cumulative\s*Total",
                    RegexOptions.IgnoreCase);
            }

            if (cumMatch.Success)
                result.CumulativeGpa = TryDec(cumMatch.Groups[1].Value);

            var termMatches = Regex.Matches(
                normalized,
                @"\bTerm\b.*?\b([0-5]\.\d{2})\b",
                RegexOptions.IgnoreCase);

            if (termMatches.Count > 0)
            {
                result.LatestTermGpa = TryDec(termMatches[^1].Groups[1].Value);
            }
            else
            {
                var tail = normalized.Length > 2500 ? normalized[^2500..] : normalized;
                var nums = Regex.Matches(tail, @"\b[0-5]\.\d{2}\b")
                    .Select(x => TryDec(x.Value))
                    .Where(x => x.HasValue && x != result.CumulativeGpa)
                    .Select(x => x!.Value)
                    .ToList();

                if (nums.Any())
                    result.LatestTermGpa = nums.Max();
            }

            var blockedPrefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "FALL", "SPRING", "SUMMER", "WINTER", "TERM", "PAGE"
        };

            var courseRegex = new Regex(@"(?<![A-Z0-9])([A-Z]{2,6})\s*[-]?\s*(\d{3,4})(?!\d)");

            foreach (Match m in courseRegex.Matches(normalized))
            {
                var prefix = m.Groups[1].Value.ToUpperInvariant();
                if (blockedPrefixes.Contains(prefix))
                    continue;

                result.CourseCodes.Add(prefix + m.Groups[2].Value);
            }

            return result;
        }

        private static (decimal? cumulativeGpa, decimal? lastTermGpa) ParseGpaFromTranscriptText(string text)
        {
            var parsed = ParseTranscriptText(text);
            return (parsed.CumulativeGpa, parsed.LatestTermGpa);
        }

        private static decimal? TryDec(string s)
        {
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
                return d;

            return null;
        }

        private static List<string> ExtractCourseCodesFromPdf(string fullPath)
        {
            var blockedPrefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "FALL", "SPRING", "SUMMER", "WINTER", "TERM", "PAGE"
        };

            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using var doc = PdfDocument.Open(fullPath);

            foreach (var page in doc.GetPages())
            {
                var words = page.GetWords().ToList();

                foreach (var word in words)
                {
                    var text = word.Text.Trim().ToUpperInvariant();
                    var m = Regex.Match(text, @"^([A-Z]{2,6})[-]?(\d{3,4})$");
                    if (m.Success)
                    {
                        var prefix = m.Groups[1].Value;
                        if (!blockedPrefixes.Contains(prefix))
                            results.Add(prefix + m.Groups[2].Value);
                    }
                }

                var lineGroups = words
                    .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 0))
                    .OrderByDescending(g => g.Key);

                foreach (var line in lineGroups)
                {
                    var tokens = line
                        .OrderBy(w => w.BoundingBox.Left)
                        .Select(w => w.Text.Trim().ToUpperInvariant())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .ToList();

                    for (int i = 0; i < tokens.Count - 1; i++)
                    {
                        if (Regex.IsMatch(tokens[i], @"^[A-Z]{2,6}$") &&
                            Regex.IsMatch(tokens[i + 1], @"^\d{3,4}$"))
                        {
                            if (!blockedPrefixes.Contains(tokens[i]))
                                results.Add(tokens[i] + tokens[i + 1]);
                        }
                    }
                }
            }

            return results.OrderBy(x => x).ToList();
        }

        private static List<string> ExtractCourseCodesByKnownTitles(string text)
        {
            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(text))
                return results.ToList();

            var normalized = Regex.Replace(text, @"\s+", " ").ToUpperInvariant();
            var titleToCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "ARABIC LANGUAGE (2)", "ARAB201" },
            { "IS APPLICATIONS DESIGN & DEVEL", "CPIS352" },
            { "HISTORY OF ASTRONOMY", "ASTR203" },
            { "PERSUASION", "ARAB292" },
            { "PRINCIPLES OF MARKETING", "MRKT260" },
            { "INFORMATION & COMPUTER SECURIT", "CPIS312" },
            { "PRINCIPLES OF HUMAN COMPUTER I", "CPIS354" },
            { "INTERNET APPLICATIONS&WEB PRO", "CPIS358" },
            { "SOFTWARE QUALITY AND TESTING", "CPIS357" },
            { "INTELLIGENT SYSTEMS", "CPIS363" },
            { "INTRODUCTION TO E-BUSINESS SYS", "CPIS380" },
            { "SUMMER(WORKPLACE) TRAINING", "CPIS323" },
            { "SYSTEMS DESIGN PATTERNS", "CPIS350" }
        };

            foreach (var item in titleToCode)
            {
                if (normalized.Contains(item.Key))
                    results.Add(item.Value);
            }

            return results.OrderBy(x => x).ToList();
        }

        private static List<string> ExtractForm5CourseCodesOnly(string text)
        {
            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(text))
                return results.ToList();

            string normalized = Regex.Replace(text.ToUpperInvariant(), @"\s+", " ");

            if (normalized.Contains("CPIS 351") || normalized.Contains("IS ANALYSIS & ARCHITECTURE DES"))
                results.Add("CPIS351");

            if (normalized.Contains("CPIS 358") || normalized.Contains("INTERNET APPLICATIONS&WEB PRO"))
                results.Add("CPIS358");

            if (normalized.Contains("CPIS 323") || normalized.Contains("SUMMER(WORKPLACE)TRAINING") || normalized.Contains("SUMMER WORKPLACE TRAINING"))
                results.Add("CPIS323");

            if (normalized.Contains("CPIS 380") || normalized.Contains("INTRODUCTION TO E-BUSINESS SYS"))
                results.Add("CPIS380");

            if (normalized.Contains("CPIS 357") || normalized.Contains("SOFTWARE QUALITY AND TESTING"))
                results.Add("CPIS357");

            if (normalized.Contains("CPIS 342") || normalized.Contains("DATA MINING"))
                results.Add("CPIS342");

            return results.ToList();
        }

        private static Dictionary<string, int> ExtractCourseHoursMapFromPdf(string fullPath, List<string>? targetCourseIds)
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

                    var lineCourseIds = ExtractCourseIdsFromTokens(tokens);
                    foreach (var courseId in lineCourseIds)
                    {
                        if (targetCourseIds != null && !targetCourseIds.Contains(courseId))
                            continue;

                        if (result.ContainsKey(courseId))
                            continue;

                        if (TryFindCourseOnLine(tokens, courseId, out var codeIndex) &&
                            TryExtractHourNearCourse(tokens, codeIndex, out var hours))
                        {
                            result[courseId] = hours;
                        }
                    }
                }
            }

            return result;
        }

        private static List<List<UglyToad.PdfPig.Content.Word>> GroupWordsIntoLines(List<UglyToad.PdfPig.Content.Word> words)
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

        private static List<string> ExtractCourseIdsFromTokens(List<string> tokens)
        {
            var result = new List<string>();

            for (int i = 0; i < tokens.Count; i++)
            {
                var m = Regex.Match(tokens[i], @"^([A-Z]{2,6})[-]?(\d{3,4})$");
                if (m.Success)
                {
                    result.Add(m.Groups[1].Value + m.Groups[2].Value);
                    continue;
                }

                if (i < tokens.Count - 1 &&
                    Regex.IsMatch(tokens[i], @"^[A-Z]{2,6}$") &&
                    Regex.IsMatch(tokens[i + 1], @"^\d{3,4}$"))
                {
                    result.Add(tokens[i] + tokens[i + 1]);
                }
            }

            return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static bool TryFindCourseOnLine(List<string> tokens, string courseId, out int codeIndex)
        {
            codeIndex = -1;

            var upperCourseId = courseId.ToUpperInvariant();
            var prefix = new string(upperCourseId.TakeWhile(char.IsLetter).ToArray());
            var number = new string(upperCourseId.SkipWhile(char.IsLetter).ToArray());

            for (int i = 0; i < tokens.Count; i++)
            {
                var m = Regex.Match(tokens[i], @"^([A-Z]{2,6})[-]?(\d{3,4})$");
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
                    Regex.IsMatch(tokens[i], @"^[A-Z]{2,6}$") &&
                    tokens[i] == prefix &&
                    tokens[i + 1] == number)
                {
                    codeIndex = i;
                    return true;
                }
            }

            return false;
        }

        private static bool TryExtractHourNearCourse(List<string> tokens, int codeIndex, out int hours)
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

        // =========================
        // Graduation Status helpers
        // =========================
        private static int ExtractCompletedHoursFromTranscriptText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            string normalized = Regex.Replace(text, @"\s+", " ").Trim();

            var patterns = new[]
            {
            @"Grand\s*Total\s*:\s*(\d{1,3})",
            @"(\d{1,3})\s*Grand\s*Total",
            @"Cumulative\s*Total\s*:\s*(\d{1,3})",
            @"(\d{1,3})\s*Cumulative\s*Total"
        };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(normalized, pattern, RegexOptions.IgnoreCase);

                if (match.Success && int.TryParse(match.Groups[1].Value, out int hours))
                {
                    if (hours >= 0 && hours <= 200)
                        return hours;
                }
            }

            return 0;
        }

        private async Task<Db.GraduationStatus> CreateOrUpdateGraduationStatusAsync(
            int studentId,
            string? extractedText,
            int fallbackCompletedHours = 0)
        {
            int completedHours = ExtractCompletedHoursFromTranscriptText(extractedText);

            if (completedHours <= 0 && fallbackCompletedHours > 0)
                completedHours = fallbackCompletedHours;

            if (completedHours < 0)
                completedHours = 0;

            if (completedHours > GraduationRequiredHours)
                completedHours = GraduationRequiredHours;

            int remainingHours = Math.Max(GraduationRequiredHours - completedHours, 0);
            string status = CalculateCurrentStatus(remainingHours);

            var graduationStatus = await _db.GraduationStatuses
                .FirstOrDefaultAsync(g => g.StudentId == studentId);

            if (graduationStatus == null)
            {
                graduationStatus = new Db.GraduationStatus
                {
                    StudentId = studentId,
                    Status = status,
                    RemainingHours = remainingHours
                };

                _db.GraduationStatuses.Add(graduationStatus);
            }
            else
            {
                graduationStatus.Status = status;
                graduationStatus.RemainingHours = remainingHours;
            }

            await _db.SaveChangesAsync();

            return graduationStatus;
        }

        // =========================
        // Student Home
        // =========================
        [HttpGet]
        public async Task<IActionResult> StudentHome()
        {
            if (HttpContext.Session.GetString("UserRole") != "Student")
                return RedirectToAction("Login", "Account");

            ViewData["Title"] = "Student Home";
            await LoadStudentSidebarDataAsync();

            int? studentId = GetCurrentStudentId();
            if (!studentId.HasValue)
                return RedirectToAction("Login", "Account");

            var student = await _db.Students.FirstOrDefaultAsync(s => s.StudentId == studentId.Value);
            if (student == null)
                return NotFound("Student not found.");

            if (!student.AdvisorId.HasValue)
                return RedirectToAction(nameof(SelectAdvisor));

            var semesterTranscriptRedirect =
    await RedirectIfTranscriptUploadRequiredForCurrentSemesterAsync(student.StudentId);

            if (semesterTranscriptRedirect != null)
                return semesterTranscriptRedirect;

            var transcriptRedirect = await RedirectIfTranscriptMissingAsync(student.StudentId);

            if (transcriptRedirect != null)
                return transcriptRedirect;

            var transcript = await _db.Transcripts
                .Include(t => t.Courses)
                .FirstOrDefaultAsync(t => t.StudentId == student.StudentId);

            int fallbackCompletedHours = transcript?.Courses?.Sum(c => c.Hours) ?? 0;

            var graduationStatus = await CreateOrUpdateGraduationStatusAsync(
                student.StudentId,
                transcript?.ExtractedInfo,
                fallbackCompletedHours);

            int remainingHours = graduationStatus.RemainingHours;
            int completedHours = Math.Max(0, GraduationRequiredHours - remainingHours);

            var model = new StudentHomeViewModel
            {
                StudentId = student.StudentId,
                StudentName = GetStringPropertyValue(student, "Name", "StudentName", "FullName"),
                StudentEmail = HttpContext.Session.GetString("UserEmail")
                    ?? GetStringPropertyValue(student, "Email", "StudentEmail", "UniversityEmail"),
                RemainingHours = remainingHours,
                CompletedHours = completedHours,
                TotalRequiredHours = GraduationRequiredHours,
                ProgressPercentage = CalculateProgressPercentage(remainingHours, GraduationRequiredHours),
                CurrentStatus = graduationStatus.Status
            };

            return View(model);
        }

        private int CalculateProgressPercentage(int remainingHours, int totalRequiredHours)
        {
            if (totalRequiredHours <= 0)
                return 0;

            if (remainingHours <= 0)
                return 100;

            int completedHours = Math.Max(0, totalRequiredHours - remainingHours);

            double percentage = ((double)completedHours / totalRequiredHours) * 100;

            int roundedPercentage = (int)Math.Round(percentage);

            if (roundedPercentage < 0)
                return 0;

            if (roundedPercentage > 100)
                return 100;

            return roundedPercentage;
        }

        private static string CalculateCurrentStatus(int remainingHours)
        {
            if (remainingHours <= 0)
                return "Graduated";

            if (remainingHours <= 11)
                return "Near Graduation";

            return "Has Remaining Courses";
        }
        // =========================
        // Select Advisor
        // =========================
        [HttpGet]
        public async Task<IActionResult> SelectAdvisor(string? search)
        {
            if (HttpContext.Session.GetString("UserRole") != "Student")
                return RedirectToAction("Login", "Account");

            ViewData["Title"] = "Select Advisor";
            await LoadStudentSidebarDataAsync();

            int? studentId = GetCurrentStudentId();
            if (!studentId.HasValue)
                return RedirectToAction("Login", "Account");

            var student = await _db.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId.Value);

            if (student == null)
                return RedirectToAction("Login", "Account");

            if (student.AdvisorId.HasValue)
            {
                var transcriptRedirect = await RedirectIfTranscriptMissingAsync(student.StudentId);
                if (transcriptRedirect != null)
                    return transcriptRedirect;

                return RedirectToAction(nameof(StudentHome));
            }

            var latestRequest = await _db.Set<Db.AdvisorRequest>()
                .Include(r => r.RequestedAdvisor)
                    .ThenInclude(a => a!.User)
                .Where(r => r.StudentId == student.StudentId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            var advisorsFromDb = await _db.Advisors
                .Include(a => a.User)
                .OrderBy(a => a.User.Name)
                .ToListAsync();

            var advisors = advisorsFromDb
                .Select(a => new AdvisorCardVM
                {
                    AdvisorId = a.AdvisorId,
                    AdvisorName = a.User != null ? a.User.Name : "",
                    AdvisorEmail = a.User != null ? a.User.Email : "",
                    Department = a.Department ?? ""
                })
                .ToList();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();

                advisors = advisors
                    .Where(a =>
                        (!string.IsNullOrWhiteSpace(a.AdvisorName) && a.AdvisorName.ToLower().Contains(search)) ||
                        (!string.IsNullOrWhiteSpace(a.AdvisorEmail) && a.AdvisorEmail.ToLower().Contains(search)) ||
                        (!string.IsNullOrWhiteSpace(a.Department) && a.Department.ToLower().Contains(search)))
                    .ToList();
            }

            var vm = new SelectAdvisorVM
            {
                StudentName = student.Name,
                Advisors = advisors,
                SearchTerm = search ?? string.Empty
            };

            if (latestRequest != null && latestRequest.Status == "Pending")
            {
                vm.HasPendingRequest = true;
                vm.PendingStatus = latestRequest.Status;
                vm.PendingAdvisorEmail = latestRequest.RequestedAdvisorEmail;
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAdvisorSelection(int advisorId)
        {
            if (HttpContext.Session.GetString("UserRole") != "Student")
                return RedirectToAction("Login", "Account");

            int? studentId = GetCurrentStudentId();
            if (!studentId.HasValue)
                return RedirectToAction("Login", "Account");

            var student = await _db.Students.FirstOrDefaultAsync(s => s.StudentId == studentId.Value);
            if (student == null)
                return RedirectToAction("Login", "Account");

            if (student.AdvisorId.HasValue)
            {
                var transcriptRedirect = await RedirectIfTranscriptMissingAsync(student.StudentId);
                if (transcriptRedirect != null)
                    return transcriptRedirect;

                return RedirectToAction(nameof(StudentHome));
            }

            var advisor = await _db.Advisors
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.AdvisorId == advisorId);

            if (advisor == null)
            {
                TempData["AdvisorError"] = "Selected advisor was not found.";
                return RedirectToAction(nameof(SelectAdvisor));
            }

            var hasPending = await _db.Set<Db.AdvisorRequest>()
                .AnyAsync(r => r.StudentId == student.StudentId && r.Status == "Pending");

            if (hasPending)
            {
                TempData["AdvisorError"] = "You already have a pending request.";
                return RedirectToAction(nameof(SelectAdvisor));
            }

            var request = new Db.AdvisorRequest
            {
                StudentId = student.StudentId,
                RequestedAdvisorId = advisor.AdvisorId,
                RequestedAdvisorEmail = advisor.User.Email,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _db.Set<Db.AdvisorRequest>().Add(request);
            await _db.SaveChangesAsync();

            await AddNotificationToAllAdminsAsync(
                senderRole: "Student",
                sourceType: "Request",
                type: "advisor selection request",
                message: $"{student.Name} sent an advisor request to {advisor.User.Name}.",
                studentId: student.StudentId);

            TempData["AdvisorSuccess"] = "Your advisor request has been sent to the admin.";
            return RedirectToAction(nameof(SelectAdvisor));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitManualAdvisorEmail(SelectAdvisorVM model)
        {
            if (HttpContext.Session.GetString("UserRole") != "Student")
                return RedirectToAction("Login", "Account");

            int? studentId = GetCurrentStudentId();
            if (!studentId.HasValue)
                return RedirectToAction("Login", "Account");

            var student = await _db.Students.FirstOrDefaultAsync(s => s.StudentId == studentId.Value);
            if (student == null)
                return RedirectToAction("Login", "Account");

            if (student.AdvisorId.HasValue)
            {
                var transcriptRedirect = await RedirectIfTranscriptMissingAsync(student.StudentId);
                if (transcriptRedirect != null)
                    return transcriptRedirect;

                return RedirectToAction(nameof(StudentHome));
            }

            if (string.IsNullOrWhiteSpace(model.ManualAdvisorEmail))
            {
                TempData["AdvisorError"] = "Please enter the advisor email.";
                return RedirectToAction(nameof(SelectAdvisor));
            }

            var hasPending = await _db.Set<Db.AdvisorRequest>()
                .AnyAsync(r => r.StudentId == student.StudentId && r.Status == "Pending");

            if (hasPending)
            {
                TempData["AdvisorError"] = "You already have a pending request.";
                return RedirectToAction(nameof(SelectAdvisor));
            }

            string email = model.ManualAdvisorEmail.Trim().ToLower();

            var advisor = await _db.Advisors
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.User.Email.ToLower() == email);

            var request = new Db.AdvisorRequest
            {
                StudentId = student.StudentId,
                RequestedAdvisorId = advisor?.AdvisorId,
                RequestedAdvisorEmail = email,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _db.Set<Db.AdvisorRequest>().Add(request);
            await _db.SaveChangesAsync();

            await AddNotificationToAllAdminsAsync(
                senderRole: "Student",
                sourceType: "Request",
                type: "manual advisor request",
                message: $"{student.Name} sent a manual advisor request for {email}.",
                studentId: student.StudentId);

            TempData["AdvisorSuccess"] = "Your request has been sent to the admin for review.";
            return RedirectToAction(nameof(SelectAdvisor));
        }

        // =========================
        // Upload Transcript
        // =========================
        [HttpGet]
        public async Task<IActionResult> UploadTranscript()
        {
            if (HttpContext.Session.GetString("UserRole") != "Student")
                return RedirectToAction("Login", "Account");

            ViewData["Title"] = "Upload Transcript";
            await LoadStudentSidebarDataAsync();

            int? studentId = GetCurrentStudentId();
            if (!studentId.HasValue)
                return RedirectToAction("Login", "Account");

            var student = await _db.Students.FirstOrDefaultAsync(s => s.StudentId == studentId.Value);
            if (student == null)
                return RedirectToAction("Login", "Account");

            if (!student.AdvisorId.HasValue)
                return RedirectToAction(nameof(SelectAdvisor));

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadTranscript(IFormFile transcriptFile)
        {
            if (HttpContext.Session.GetString("UserRole") != "Student")
                return RedirectToAction("Login", "Account");

            ViewData["Title"] = "Upload Transcript";
            await LoadStudentSidebarDataAsync();

            if (transcriptFile == null || transcriptFile.Length == 0)
            {
                ViewBag.Error = "Please select a PDF file.";
                return View();
            }

            var ext = Path.GetExtension(transcriptFile.FileName).ToLowerInvariant();
            if (ext != ".pdf")
            {
                ViewBag.Error = "Only PDF files are allowed.";
                return View();
            }

            if (!IsRealPdfFile(transcriptFile))
            {
                ViewBag.Error = "The uploaded file is not a valid PDF file.";
                return View();
            }

            int? studentIdSession = GetCurrentStudentId();
            if (!studentIdSession.HasValue)
            {
                ViewBag.Error = "Student session is not found. Please login again.";
                return View();
            }

            int studentId = studentIdSession.Value;

            var student = await _db.Students.FirstOrDefaultAsync(s => s.StudentId == studentId);
            if (student == null)
            {
                ViewBag.Error = "Student record is not found in the database.";
                return View();
            }

            if (!student.AdvisorId.HasValue)
                return RedirectToAction(nameof(SelectAdvisor));

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "transcripts");
            Directory.CreateDirectory(uploadsFolder);

            var savedFileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(uploadsFolder, savedFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await transcriptFile.CopyToAsync(stream);
            }

            string extractedText;
            try
            {
                extractedText = ReadPdfText(fullPath);
            }
            catch
            {
                DeleteFileIfExists(fullPath);
                ViewBag.Error = "The uploaded PDF could not be read. Please upload a valid transcript PDF.";
                return View();
            }

            var parsedTranscript = ParseTranscriptText(extractedText);
            var (cumulativeGpa, latestTermGpa) = ParseGpaFromTranscriptText(extractedText);

            var pdfCourseCodes = ExtractCourseCodesFromPdf(fullPath);
            var titleBasedCodes = ExtractCourseCodesByKnownTitles(extractedText);
            var form5OnlyCodes = ExtractForm5CourseCodesOnly(extractedText);
            var courseCodesSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var code in parsedTranscript.CourseCodes
                         .Concat(pdfCourseCodes)
                         .Concat(titleBasedCodes)
                         .Concat(form5OnlyCodes))
            {
                var cleanCode = code?.Trim().ToUpperInvariant();
                if (!string.IsNullOrWhiteSpace(cleanCode))
                    courseCodesSet.Add(cleanCode);
            }

            courseCodesSet.RemoveWhere(code =>
    string.IsNullOrWhiteSpace(code) ||
    code.StartsWith("FALL", StringComparison.OrdinalIgnoreCase) ||
    code.StartsWith("SPRING", StringComparison.OrdinalIgnoreCase) ||
    code.StartsWith("SUMMER", StringComparison.OrdinalIgnoreCase) ||
    code.StartsWith("WINTER", StringComparison.OrdinalIgnoreCase) ||
    code.StartsWith("TERM", StringComparison.OrdinalIgnoreCase) ||
    code.StartsWith("PAGE", StringComparison.OrdinalIgnoreCase));

            bool isTranscript = LooksLikeTranscript(
                extractedText,
                courseCodesSet,
                cumulativeGpa,
                latestTermGpa);

            if (!isTranscript)
            {
                DeleteFileIfExists(fullPath);
                ViewBag.Error = "The uploaded PDF does not appear to be a valid academic transcript. Please upload your official transcript.";
                return View();
            }

            var transcriptHourMap = ExtractCourseHoursMapFromPdf(fullPath, courseCodesSet.ToList());

            ViewBag.DebugStudentId = studentId;
            ViewBag.DebugOriginalFileName = transcriptFile.FileName;
            ViewBag.CodesCount = courseCodesSet.Count;
            ViewBag.CodesPreview = string.Join(", ", courseCodesSet.OrderBy(x => x));
            ViewBag.DebugPreview = extractedText.Length > 1000 ? extractedText[..1000] : extractedText;

            var transcript = await _db.Transcripts
                .Include(t => t.Courses)
                .FirstOrDefaultAsync(t => t.StudentId == studentId);

            if (transcript == null)
            {
                transcript = new Db.Transcript { StudentId = studentId };
                _db.Transcripts.Add(transcript);
                await _db.SaveChangesAsync();
                await _db.Entry(transcript).Collection(t => t.Courses).LoadAsync();
            }

            transcript.PdfFile = $"/uploads/transcripts/{savedFileName}";
            transcript.ExtractedInfo = extractedText;
            transcript.UploadedAt = DateTime.Now;

            if (cumulativeGpa.HasValue)
                transcript.Gpa = cumulativeGpa.Value;
            else if (parsedTranscript.CumulativeGpa.HasValue)
                transcript.Gpa = parsedTranscript.CumulativeGpa.Value;

            if (latestTermGpa.HasValue)
                transcript.SemesterGpa = latestTermGpa.Value;
            else if (parsedTranscript.LatestTermGpa.HasValue)
                transcript.SemesterGpa = parsedTranscript.LatestTermGpa.Value;

            transcript.ExtractedCourses = courseCodesSet.Count == 0
                ? null
                : string.Join(", ", courseCodesSet.OrderBy(x => x));

            transcript.Courses.Clear();

            if (courseCodesSet.Count > 0)
            {
                var coursesInDb = await _db.Courses
                    .Where(c => courseCodesSet.Contains(c.CourseId.Trim().ToUpper()))
                    .ToListAsync();

                foreach (var course in coursesInDb)
                {
                    var normalizedCourseId = course.CourseId.Trim().ToUpperInvariant();
                    bool isUnclassified = string.IsNullOrWhiteSpace(course.RequirementCategory);
                    bool hasZeroHours = course.Hours <= 0;

                    if ((isUnclassified || hasZeroHours) &&
                        transcriptHourMap.TryGetValue(normalizedCourseId, out var extractedHours) &&
                        extractedHours > 0)
                    {
                        course.Hours = extractedHours;
                    }
                }

                var existingIds = coursesInDb
                    .Select(c => c.CourseId.Trim().ToUpperInvariant())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var missingIds = courseCodesSet
                    .Where(id => !existingIds.Contains(id))
                    .ToList();

                foreach (var id in missingIds)
                {
                    var newCourse = new Db.Course
                    {
                        CourseId = id,
                        CourseName = id,
                        Hours = transcriptHourMap.TryGetValue(id, out var extractedHours) ? extractedHours : 0
                    };

                    _db.Courses.Add(newCourse);
                    coursesInDb.Add(newCourse);
                }

                foreach (var course in coursesInDb)
                    transcript.Courses.Add(course);
            }

            await _db.SaveChangesAsync();

            int fallbackCompletedHours = transcript.Courses?.Sum(c => c.Hours) ?? 0;

            await CreateOrUpdateGraduationStatusAsync(
                student.StudentId,
                extractedText,
                fallbackCompletedHours);

            await CreateOrUpdateForm5AfterTranscriptUploadAsync(student, courseCodesSet);

            await AddNotificationAsync(
                senderRole: "System",
                sourceType: "Recommendation",
                type: "initial recommendation",
                message: "Your initial recommendation is ready after transcript upload.",
                studentId: student.StudentId);
            if (student.AdvisorId.HasValue)
            {
                await AddNotificationAsync(
                    senderRole: "Student",
                    sourceType: "Transcript",
                    type: "transcript uploaded",
                    message: $"{student.Name} uploaded the transcript.",
                    studentId: student.StudentId,
                    advisorId: student.AdvisorId.Value);
            }
            else
            {
                await AddNotificationToAllAdminsAsync(
                    senderRole: "Student",
                    sourceType: "Transcript",
                    type: "transcript uploaded",
                    message: $"{student.Name} uploaded the transcript and is waiting for advisor assignment.",
                    studentId: student.StudentId);
            }

            if (!student.AdvisorId.HasValue)
                return RedirectToAction(nameof(SelectAdvisor));

            return RedirectToAction(nameof(StudentHome));
        }

       


        // =========================
        // Student Chat
        // =========================

        private async Task<(Db.Meeting meeting, Db.Student student, string advisorName)?> GetOrCreateStudentMeetingAsync()
        {
            int? studentId = GetCurrentStudentId();

            if (!studentId.HasValue)
                return null;

            var student = await _db.Students
                .Include(s => s.Advisor)
                    .ThenInclude(a => a!.User)
                .FirstOrDefaultAsync(s => s.StudentId == studentId.Value);

            if (student == null)
                return null;

            if (!student.AdvisorId.HasValue || student.Advisor == null)
                return null;

            var meeting = await _db.Meetings
                .Where(m => m.StudentId == student.StudentId && m.AdvisorId == student.AdvisorId.Value)
                .OrderByDescending(m => m.MeetingId)
                .FirstOrDefaultAsync();

            if (meeting == null)
            {
                meeting = new Db.Meeting
                {
                    StudentId = student.StudentId,
                    AdvisorId = student.AdvisorId.Value,
                    StartTime = DateTime.Now,
                    IsRecordingStarted = false,
                    LastRecordingAction = null
                };

                _db.Meetings.Add(meeting);
                await _db.SaveChangesAsync();
            }

            string advisorName = student.Advisor.User?.Name ?? "Advisor";

            return (meeting, student, advisorName);
        }

        [HttpGet]
        public async Task<IActionResult> Chat()
        {
            if (HttpContext.Session.GetString("UserRole") != "Student")
                return RedirectToAction("Login", "Account");

            ViewData["Title"] = "Chat";
            await LoadStudentSidebarDataAsync();

            var data = await GetOrCreateStudentMeetingAsync();

            if (data == null)
                return RedirectToAction(nameof(SelectAdvisor));

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

            var messages = await _db.MeetingMessages
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(string message)
        {
            if (HttpContext.Session.GetString("UserRole") != "Student")
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(message))
                return RedirectToAction(nameof(Chat));

            var data = await GetOrCreateStudentMeetingAsync();

            if (data == null)
                return RedirectToAction(nameof(SelectAdvisor));

            var meeting = data.Value.meeting;
            var student = data.Value.student;

            var newMessage = new Db.MeetingMessage
            {
                MeetingId = meeting.MeetingId,
                SenderName = student.Name,
                MessageText = message.Trim(),
                MessageDate = DateTime.Now,
                IsRecorded = meeting.IsRecordingStarted
            };

            _db.MeetingMessages.Add(newMessage);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Chat));
        }// =========================
         // Community Student
         // =========================
        [HttpGet]
        public async Task<IActionResult> CommunityStudent()
        {
            if (HttpContext.Session.GetString("UserRole") != "Student")
                return RedirectToAction("Login", "Account");

            ViewData["Title"] = "Community Student";
            await LoadStudentSidebarDataAsync();

            int? studentId = GetCurrentStudentId();
            if (!studentId.HasValue)
                return RedirectToAction("Login", "Account");

            var transcriptRedirect = await RedirectIfTranscriptMissingAsync(studentId.Value);
            if (transcriptRedirect != null)
                return transcriptRedirect;

            var currentUserName = await _db.Students
                .Where(s => s.StudentId == studentId.Value)
                .Select(s => s.Name)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(currentUserName))
                currentUserName = HttpContext.Session.GetString("UserName") ?? "Student";

            ViewBag.CurrentUserName = currentUserName;
            var rawMessages = await _db.CommunityMessages
                .Where(m => m.CommunityId == 1)
                .OrderBy(m => m.MessageDate)
                .ToListAsync();

            var dbMessages = rawMessages.Select(m => new CommunityMessageVM
            {
                SenderName = m.SenderName,
                MessageText = m.MessageText,
                ImagePath = "~/images/user.png",
                SenderInitials = string.IsNullOrEmpty(m.SenderName) ? "U" :
                                 string.Concat(m.SenderName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                       .Select(n => n[0])).ToUpper(),
                IsCurrentUserMessage = m.SenderName == currentUserName,
                BubbleColorClass = m.SenderName != null && m.SenderName.Contains("Amina") ? "msg-purple" : "msg-blue"
            }).ToList();

            var dbMembers = await _db.Students
                .Select(s => new CommunityMemberVM
                {
                    Name = s.Name,
                    ImagePath = "~/images/user.png"
                })
                .ToListAsync();

            var model = new CommunityStudentVM
            {
                Messages = dbMessages,
                Members = dbMembers
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SendStudentMessage([FromBody] SendStudentMessageRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { success = false, message = "الرسالة فارغة" });

            var userIdFromSession = HttpContext.Session.GetInt32("UserId");

            var studentName = await _db.Students
                .Where(s => s.UserId == userIdFromSession)
                .Select(s => s.Name)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(studentName))
            {
                studentName = HttpContext.Session.GetString("UserName") ?? "Student";
            }

            try
            {
                var newMessage = new Db.CommunityMessage
                {
                    CommunityId = 1,
                    SenderName = studentName,
                    MessageText = request.Message.Trim(),
                    MessageDate = DateTime.Now
                };

                _db.CommunityMessages.Add(newMessage);
                await _db.SaveChangesAsync();

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

        public class SendStudentMessageRequest
        {
            public int CommunityId { get; set; }
            public string Message { get; set; } = string.Empty;
        }

        // =========================
        // Course Recommendation
        // =========================
        [HttpGet]
        public async Task<IActionResult> CourseRecommendation()
        {
            if (HttpContext.Session.GetString("UserRole") != "Student")
                return RedirectToAction("Login", "Account");

            await LoadStudentSidebarDataAsync();

            int? studentId = GetCurrentStudentId();
            if (!studentId.HasValue)
                return RedirectToAction("Login", "Account");

            var transcriptRedirect = await RedirectIfTranscriptMissingAsync(studentId.Value);
            if (transcriptRedirect != null)
                return transcriptRedirect;

            var student = await _db.Students
                .Include(s => s.Transcript)
                    .ThenInclude(t => t.Courses)
                .FirstOrDefaultAsync(s => s.StudentId == studentId.Value);

            if (student == null)
                return NotFound("Student not found.");

            ViewBag.StudentName = student.Name;
            ViewBag.StudentEmail = HttpContext.Session.GetString("UserEmail") ?? "student@kau.edu.sa";
            ViewBag.ActivePage = "CourseRecommendation";

            var transcript = student.Transcript;

            var selected = await GetSelectedCoursesFromForm2Async(student.StudentId);

            var latestForm2 = await _db.Forms
             .Where(f => f.StudentId == student.StudentId && f.FormType == "Form 2")
             .OrderByDescending(f => f.FormDate)
             .ThenByDescending(f => f.FormId)
             .FirstOrDefaultAsync();

            ViewBag.AdvisorNotes = latestForm2?.AdvisorNotes ?? "";

            var cardsJson = HttpContext.Session.GetString("RecommendedCards");
            var cards = string.IsNullOrEmpty(cardsJson)
                ? new List<CourseCardVM>()
                : JsonSerializer.Deserialize<List<CourseCardVM>>(cardsJson) ?? new List<CourseCardVM>();

            var model = new CourseRecommendationViewModel
            {
                StudentName = student.Name,
                StudentId = student.StudentId,
                Gpa = transcript?.Gpa,
                SemesterGpa = transcript?.SemesterGpa,
                Selected = selected,
                Cards = cards,
                FreeElectiveCourse1 = HttpContext.Session.GetString("FreeElectiveCourse1") ?? "",
                FreeElectiveCourse2 = HttpContext.Session.GetString("FreeElectiveCourse2") ?? "",
                FreeElectiveCourse3 = HttpContext.Session.GetString("FreeElectiveCourse3") ?? ""
            };

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectRecommendedCourse(string courseId, string? manualCourseValue = null)
        {
            int? studentId = GetCurrentStudentId();

            if (!studentId.HasValue)
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(courseId))
                return RedirectToAction(nameof(CourseRecommendation));

            string normalizedCourseId = NormalizeCourseId(courseId);

            var selectedCourses = await GetSelectedCoursesFromForm2Async(studentId.Value);

            int hours = 0;
            string selectedCourseId = normalizedCourseId;

            CourseCardVM? card = null;

            var cardsJson = HttpContext.Session.GetString("RecommendedCards");
            if (!string.IsNullOrWhiteSpace(cardsJson))
            {
                var cards = JsonSerializer.Deserialize<List<CourseCardVM>>(cardsJson) ?? new List<CourseCardVM>();

                card = cards.FirstOrDefault(c =>
                    NormalizeCourseId(c.CourseId) == normalizedCourseId);

                if (card != null)
                    hours = card.Hours;
            }

            Db.Course? dbCourse = null;

            if (hours <= 0)
            {
                dbCourse = await _db.Courses
                    .FirstOrDefaultAsync(c =>
                        c.CourseId.Trim()
                            .ToUpper()
                            .Replace(" ", "")
                            .Replace("-", "")
                            .Replace("_", "")
                            .Replace("/", "") == normalizedCourseId);

                if (dbCourse != null)
                    hours = dbCourse.Hours;
            }

            bool isFreeOrElective =
                card != null && IsFreeOrElectiveCourse(card.CourseId, card.CourseName, null);

            if (!isFreeOrElective && dbCourse != null)
            {
                isFreeOrElective = IsFreeOrElectiveCourse(
                    dbCourse.CourseId,
                    dbCourse.CourseName,
                    dbCourse.RequirementCategory);
            }

            if (isFreeOrElective)
            {
                var typedCourseId = NormalizeCourseId(manualCourseValue);

                if (string.IsNullOrWhiteSpace(typedCourseId))
                {
                    TempData["ErrorMessage"] = "Please enter the free or elective course code first.";
                    return RedirectToAction(nameof(CourseRecommendation));
                }

                selectedCourseId = typedCourseId;

                var category = GetManualCourseCategory(
                    card?.CourseId ?? courseId,
                    card?.CourseName,
                    dbCourse?.RequirementCategory);

                var manualCourse = await _db.Courses
                    .FirstOrDefaultAsync(c =>
                        c.CourseId.Trim()
                            .ToUpper()
                            .Replace(" ", "")
                            .Replace("-", "")
                            .Replace("_", "")
                            .Replace("/", "") == selectedCourseId);

                if (manualCourse == null)
                {
                    manualCourse = new Db.Course
                    {
                        CourseId = selectedCourseId,
                        CourseName = selectedCourseId,
                        Hours = hours > 0 ? hours : 3,
                        Prerequisite = null,
                        RequirementCategory = category
                    };

                    _db.Courses.Add(manualCourse);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    if (manualCourse.Hours <= 0)
                        manualCourse.Hours = hours > 0 ? hours : 3;

                    if (string.IsNullOrWhiteSpace(manualCourse.RequirementCategory))
                        manualCourse.RequirementCategory = category;

                    await _db.SaveChangesAsync();
                }

                hours = manualCourse.Hours;
            }

            if (hours <= 0)
                hours = 3;

            if (!selectedCourses.Any(c => NormalizeCourseId(c.CourseId) == selectedCourseId))
            {
                selectedCourses.Add(new SelectedCourseVM
                {
                    CourseId = selectedCourseId,
                    Hours = hours
                });
            }

            selectedCourses = selectedCourses
                .Where(c => !string.IsNullOrWhiteSpace(c.CourseId))
                .GroupBy(c => NormalizeCourseId(c.CourseId))
                .Select(g => g.First())
                .ToList();

            await SaveSelectedCoursesToForm2Async(studentId.Value, selectedCourses, "Draft");

           

            TempData["Success"] = $"Course {selectedCourseId} selected successfully.";
            return RedirectToAction(nameof(CourseRecommendation));
        }
        [HttpPost]
        public async Task<IActionResult> RemoveRecommendedCourse(string courseId)
        {
            int? studentId = GetCurrentStudentId();

            if (!studentId.HasValue)
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(courseId))
                return RedirectToAction(nameof(CourseRecommendation));

            string normalizedCourseId = NormalizeCourseId(courseId);

            var selectedCourses = await GetSelectedCoursesFromForm2Async(studentId.Value);

            selectedCourses = selectedCourses
                .Where(c => NormalizeCourseId(c.CourseId) != normalizedCourseId)
                .ToList();

            await SaveSelectedCoursesToForm2Async(studentId.Value, selectedCourses, "Draft");

            

            TempData["Success"] = "Course removed successfully.";
            return RedirectToAction(nameof(CourseRecommendation));
        }
        [HttpPost]
        public async Task<IActionResult> SendCourseRecommendationToAdvisor()
        {
            int? studentId = GetCurrentStudentId();

            if (!studentId.HasValue)
                return RedirectToAction("Login", "Account");

            var transcriptRedirect = await RedirectIfTranscriptMissingAsync(studentId.Value);
            if (transcriptRedirect != null)
                return transcriptRedirect;

            var student = await _db.Students
                .Include(s => s.Transcript)
                    .ThenInclude(t => t.Courses)
                .FirstOrDefaultAsync(s => s.StudentId == studentId.Value);

            if (student == null)
            {
                TempData["ErrorMessage"] = "Student not found.";
                return RedirectToAction(nameof(CourseRecommendation));
            }

            int? advisorId = student.AdvisorId;

            if (!advisorId.HasValue)
            {
                TempData["ErrorMessage"] = "No advisor is assigned to this student.";
                return RedirectToAction(nameof(SelectAdvisor));
            }

            var selectedCourses = await GetSelectedCoursesWithManualCoursesAsync(studentId.Value);

            await SaveSelectedCoursesToForm2Async(studentId.Value, selectedCourses, "Sent");

            int advisedHours = selectedCourses.Sum(x => x.Hours);

            string extractedInfo = student.Transcript?.ExtractedInfo ?? string.Empty;
            string currentSemester = ExtractLastAcademicTerm(extractedInfo);

            int earnedHours = student.Transcript?.Courses?.Sum(c => c.Hours) ?? 0;

            var latestForm = await _db.Forms
                .Include(f => f.CourseChoiceMonitoringForm)
                .Where(f => f.StudentId == student.StudentId && f.FormType == "Form 2")
                .OrderByDescending(f => f.FormId)
                .FirstOrDefaultAsync();

            Db.Form form;
            Db.CourseChoiceMonitoringForm form2;

            if (latestForm == null)
            {
                form = new Db.Form
                {
                    StudentId = student.StudentId,
                    AdvisorId = advisorId.Value,
                    FormType = "Form 2",
                    FormDate = DateTime.Now,
                    FormStatus = "Sent",
                    AdvisorNotes = null,
                    AutoFilled = true,
                    AdvisorConfirmation = null
                };

                _db.Forms.Add(form);
                await _db.SaveChangesAsync();

                form2 = new Db.CourseChoiceMonitoringForm
                {
                    FormId = form.FormId
                };

                _db.CourseChoiceMonitoringForms.Add(form2);
            }
            else
            {
                form = latestForm;

                form2 = latestForm.CourseChoiceMonitoringForm ?? new Db.CourseChoiceMonitoringForm
                {
                    FormId = form.FormId
                };

                if (latestForm.CourseChoiceMonitoringForm == null)
                    _db.CourseChoiceMonitoringForms.Add(form2);
            }

            form.FormDate = DateTime.Now;
            form.FormStatus = "Sent";
            form.AutoFilled = true;
            form.AdvisorConfirmation = null;

            form2.Semester = currentSemester;
            form2.ComingSemester = GetNextSemester(currentSemester);
            form2.RunningCreditHours = earnedHours;
            form2.AdvisedCreditHours = advisedHours;
            form2.Level = CalculateLevelFromHours(earnedHours);
            form2.DropSubjects = ExtractDropSubjects(extractedInfo);
            form2.ICSubjects = ExtractICSubjects(extractedInfo);
            form2.IpSubjects = ExtractIPSubjects(extractedInfo);
            form2.SelectedCoursesJson = JsonSerializer.Serialize(selectedCourses);

            await _db.SaveChangesAsync();

            await AddRecommendationNotificationToAdvisorAsync(
                student.StudentId,
                "course recommendation sent",
                "sent course recommendations for advisor review.");

            TempData["Success"] = "Course recommendation sent successfully.";
            return RedirectToAction(nameof(CourseRecommendation));
        }
        [HttpPost]
        public async Task<IActionResult> UploadTranscriptForRecommendation(IFormFile transcriptFile)
        {
            if (transcriptFile == null || transcriptFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please upload a transcript file.";
                return RedirectToAction(nameof(CourseRecommendation));
            }

            try
            {
                int? studentId = GetCurrentStudentId();

                if (!studentId.HasValue)
                    return RedirectToAction("Login", "Account");

                var student = await _db.Students
                    .Include(s => s.Transcript)
                    .FirstOrDefaultAsync(s => s.StudentId == studentId.Value);

                if (student == null)
                {
                    TempData["ErrorMessage"] = "Student not found.";
                    return RedirectToAction(nameof(CourseRecommendation));
                }

                int planId = 1;

                var parserCourses = await _transcriptParserService.ParseTranscriptAsync(transcriptFile)
                    ?? new List<TranscriptCourseItem>();

                var aiCourses = await _transcriptAiParserService.ParseTranscriptAsync(transcriptFile)
                    ?? new List<TranscriptCourseItem>();

                var validCourseIds = await _db.StudyPlanCourses
                    .Where(x => x.PlanId == planId)
                    .Select(x => x.CourseId)
                    .ToListAsync();

                var validCourseIdSet = validCourseIds
                    .Select(id => NormalizeCourseId(id))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var transcriptCourses = MergeTranscriptCourses(parserCourses, aiCourses, validCourseIdSet);

                var allTranscriptCourses = MergeAllTranscriptCoursesForRecommendation(
                    parserCourses,
                    aiCourses
                );
                var debugPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "debug_recommendation_courses.txt"
                );

                Directory.CreateDirectory(Path.GetDirectoryName(debugPath)!);

                var debugLines = new List<string>
        {
            $"Parser courses count: {parserCourses?.Count ?? 0}",
            $"AI courses count: {aiCourses?.Count ?? 0}",
            $"Final merged courses count: {transcriptCourses?.Count ?? 0}",
            "----------------------------------------"
        };

                debugLines.AddRange(
                    transcriptCourses.Select(c =>
                        $"{c.CourseId} | Grade: {c.Grade} | IsPassed: {c.IsPassed}"
                    )
                );

                System.IO.File.WriteAllLines(debugPath, debugLines);

                TempData["ParsedCoursesDebug"] = transcriptCourses.Count == 0
                    ? $"No matched courses for Plan {planId}."
                    : $"Found {transcriptCourses.Count} courses.";

                if (!transcriptCourses.Any())
                {
                    HttpContext.Session.Remove("RecommendedCards");
                    TempData["ErrorMessage"] = "لم يتم العثور على مواد تطابق خطتك الدراسية في الملف.";
                    return RedirectToAction(nameof(CourseRecommendation));
                }

                var recommendations = await _recommendationEngineService
                    .GenerateRecommendationsAsync(planId, transcriptCourses);

                var cards = await BuildCourseRecommendationCardsAsync(
    planId,
    recommendations ?? new List<RecommendedCourseVm>(),
    transcriptCourses,
    allTranscriptCourses
);

                HttpContext.Session.SetString("RecommendedCards", JsonSerializer.Serialize(cards));

                await SaveSelectedCoursesToForm2Async(
                    studentId.Value,
                    new List<SelectedCourseVM>(),
                    "Draft"
                );

                TempData["Success"] = "تمت المعالجة بنجاح.";
                return RedirectToAction(nameof(CourseRecommendation));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Technical Error: {ex.Message}";
                return RedirectToAction(nameof(CourseRecommendation));
            }
        }
        private static bool IsFreeOrElectiveCourse(string? courseId, string? courseName, string? requirementCategory)
        {
            var code = courseId ?? "";
            var name = courseName ?? "";
            var category = requirementCategory ?? "";

            return code.Contains("FREE", StringComparison.OrdinalIgnoreCase) ||
                   code.Contains("ELEC", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("Free", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("Elective", StringComparison.OrdinalIgnoreCase) ||
                   category.Contains("Free", StringComparison.OrdinalIgnoreCase) ||
                   category.Contains("Elective", StringComparison.OrdinalIgnoreCase);
        }
        private static string GetManualCourseCategory(
      string? courseId,
      string? courseName,
      string? requirementCategory)
        {
            return "Free/Elective";
        }
        private static bool ArePrerequisitesCompleted(
    string? prerequisite,
    HashSet<string> passedCourseIds,
    int completedHours = 0)
        {
            if (string.IsNullOrWhiteSpace(prerequisite))
                return true;

            var text = prerequisite.Trim();

            if (text == "-" || text == "---")
                return true;

            if (text.Contains("Department Approval", StringComparison.OrdinalIgnoreCase))
                return false;

            if (text.Contains("Senior Level", StringComparison.OrdinalIgnoreCase))
                return completedHours >= 100;

            var matches = Regex.Matches(
                text.ToUpperInvariant(),
                @"([A-Z]{2,6})\s*[-]?\s*(\d{3,4})"
            );

            if (matches.Count == 0)
                return true;

            foreach (Match match in matches)
            {
                var requiredCourseId = NormalizeCourseId(
                    match.Groups[1].Value + match.Groups[2].Value
                );

                if (!passedCourseIds.Contains(requiredCourseId))
                    return false;
            }

            return true;
        }
        private static List<TranscriptCourseItem> MergeAllTranscriptCoursesForRecommendation(
    List<TranscriptCourseItem> parserCourses,
    List<TranscriptCourseItem> aiCourses)
        {
            var allCourses = new List<TranscriptCourseItem>();

            if (parserCourses != null)
                allCourses.AddRange(parserCourses);

            if (aiCourses != null)
                allCourses.AddRange(aiCourses);

            return allCourses
                .Where(c => !string.IsNullOrWhiteSpace(c.CourseId))
                .Select(c =>
                {
                    c.CourseId = NormalizeCourseId(c.CourseId);
                    c.Grade = NormalizeGrade(c.Grade);
                    c.IsPassed = c.IsPassed || IsPassingGrade(c.Grade);
                    return c;
                })
                .GroupBy(c => c.CourseId)
                .Select(g => g
                    .OrderByDescending(c => c.IsPassed)
                    .First())
                .ToList();
        }

        private static bool IsFreeOrElectiveSlot(
            string? courseId,
            string? courseName,
            string? requirementCategory)
        {
            var code = NormalizeCourseId(courseId);
            var name = courseName ?? "";
            var category = requirementCategory ?? "";

            return code.Contains("COLFREE", StringComparison.OrdinalIgnoreCase) ||
                   code.Contains("DEPTELEC", StringComparison.OrdinalIgnoreCase) ||
                   code.Contains("FREE", StringComparison.OrdinalIgnoreCase) ||
                   code.Contains("ELEC", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("College Free", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("Department Elective", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("Free", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("Elective", StringComparison.OrdinalIgnoreCase) ||
                   category.Contains("Free", StringComparison.OrdinalIgnoreCase) ||
                   category.Contains("Elective", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetCoursePrefix(string? courseId)
        {
            var normalized = NormalizeCourseId(courseId);

            if (string.IsNullOrWhiteSpace(normalized))
                return "";

            return new string(normalized.TakeWhile(char.IsLetter).ToArray());
        }

        private static bool IsActualFreeOrElectiveTakenCourse(
            string courseId,
            HashSet<string> requiredPlanCourseIds)
        {
            var normalizedId = NormalizeCourseId(courseId);

            if (string.IsNullOrWhiteSpace(normalizedId))
                return false;

            // If the course is already required in the IS plan, it is not counted as free/elective.
            if (requiredPlanCourseIds.Contains(normalizedId))
                return false;

            // Placeholder courses are not real completed courses.
            if (IsFreeOrElectiveSlot(normalizedId, null, null))
                return false;

            var prefix = GetCoursePrefix(normalizedId);

            // These are foundation or general preparation courses.
            // They should not be counted as free/elective.
            var excludedPrefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "ELIS",
        "COMM",
        "MATH",
        "PHYS",
        "BIO",
        "BIOC",
        "CHEM",
        "STAT",
        "CPIT",
        "CPCS",
        "ISLS"
    };

            if (excludedPrefixes.Contains(prefix))
                return false;

            return true;
        }

        private static int GetCourseHoursOrDefault(
            string courseId,
            Dictionary<string, Db.Course> courseMap)
        {
            var normalizedId = NormalizeCourseId(courseId);

            if (courseMap.TryGetValue(normalizedId, out var course) && course.Hours > 0)
                return course.Hours;

            return 3;
        }

        private static int CountCompletedFreeElectiveHours(
            HashSet<string> passedCourseIds,
            HashSet<string> requiredPlanCourseIds,
            Dictionary<string, Db.Course> courseMap)
        {
            int completedHours = 0;

            foreach (var courseId in passedCourseIds)
            {
                var normalizedId = NormalizeCourseId(courseId);

                if (!IsActualFreeOrElectiveTakenCourse(normalizedId, requiredPlanCourseIds))
                    continue;

                completedHours += GetCourseHoursOrDefault(normalizedId, courseMap);
            }

            // The required free/elective total is 9 hours only.
            return Math.Min(completedHours, 9);
        }
        private async Task<List<CourseCardVM>> BuildCourseRecommendationCardsAsync(
    int planId,
    List<RecommendedCourseVm> recommendations,
    List<TranscriptCourseItem> transcriptCourses,
    List<TranscriptCourseItem> allTranscriptCourses)
        {
            const int MaxRecommendedCards = 6;
            const int MoveToNextBlockWhenCountIs = 3;
            const int RequiredFreeElectiveHours = 9;

            // We do not mix ninth semester with tenth semester.
            const int LastSemesterAllowedToPullFromNext = 8;

            var passedCourseIds = allTranscriptCourses
                .Where(c => c.IsPassed)
                .Select(c => NormalizeCourseId(c.CourseId))
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!passedCourseIds.Any())
            {
                passedCourseIds = transcriptCourses
                    .Where(c => c.IsPassed)
                    .Select(c => NormalizeCourseId(c.CourseId))
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }

            var planCourses = await _db.StudyPlanCourses
                .Where(x => x.PlanId == planId)
                .OrderBy(x => x.SemesterNo)
                .ThenBy(x => x.DisplayOrder)
                .ToListAsync();

            var allCourses = await _db.Courses.ToListAsync();

            var courseMap = allCourses
                .Where(c => !string.IsNullOrWhiteSpace(c.CourseId))
                .GroupBy(c => NormalizeCourseId(c.CourseId))
                .ToDictionary(
                    g => g.Key,
                    g => g.First(),
                    StringComparer.OrdinalIgnoreCase
                );

            var requiredPlanCourseIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var planCourse in planCourses)
            {
                var planCourseId = NormalizeCourseId(planCourse.CourseId);

                if (string.IsNullOrWhiteSpace(planCourseId))
                    continue;

                if (!courseMap.TryGetValue(planCourseId, out var course))
                    continue;

                if (IsFreeOrElectiveSlot(course.CourseId, course.CourseName, course.RequirementCategory))
                    continue;

                requiredPlanCourseIds.Add(planCourseId);
            }

            int completedFreeElectiveHours = CountCompletedFreeElectiveHours(
                passedCourseIds,
                requiredPlanCourseIds,
                courseMap
            );

            int coveredFreeElectiveHours = completedFreeElectiveHours;
            int remainingFreeElectiveHours = Math.Max(RequiredFreeElectiveHours - completedFreeElectiveHours, 0);

            int completedHours = allCourses
                .Where(c => passedCourseIds.Contains(NormalizeCourseId(c.CourseId)))
                .Sum(c => Math.Max(c.Hours, 0));

            var candidates = new List<(int SemesterNo, int DisplayOrder, Db.Course Course)>();

            foreach (var planCourse in planCourses)
            {
                var planCourseId = NormalizeCourseId(planCourse.CourseId);

                if (string.IsNullOrWhiteSpace(planCourseId))
                    continue;

                if (!courseMap.TryGetValue(planCourseId, out var course))
                    continue;

                var courseId = NormalizeCourseId(course.CourseId);

                if (string.IsNullOrWhiteSpace(courseId))
                    continue;

                bool isFreeOrElectiveSlot = IsFreeOrElectiveSlot(
                    course.CourseId,
                    course.CourseName,
                    course.RequirementCategory);

                int courseHours = course.Hours > 0 ? course.Hours : 3;

                if (isFreeOrElectiveSlot)
                {
                    if (coveredFreeElectiveHours > 0)
                    {
                        coveredFreeElectiveHours -= courseHours;
                        continue;
                    }

                    if (remainingFreeElectiveHours <= 0)
                        continue;

                    candidates.Add((
                        planCourse.SemesterNo,
                        planCourse.DisplayOrder,
                        course
                    ));

                    remainingFreeElectiveHours -= courseHours;
                    continue;
                }

                if (passedCourseIds.Contains(courseId))
                    continue;

                if (!ArePrerequisitesCompleted(course.Prerequisite, passedCourseIds, completedHours))
                    continue;

                candidates.Add((
                    planCourse.SemesterNo,
                    planCourse.DisplayOrder,
                    course
                ));
            }

            var cards = new List<CourseCardVM>();

            if (!candidates.Any())
                return cards;

            var currentSemesterNo = candidates
                .OrderBy(c => c.SemesterNo)
                .ThenBy(c => c.DisplayOrder)
                .Select(c => c.SemesterNo)
                .First();

            var currentBlockCandidates = candidates
                .Where(c => c.SemesterNo == currentSemesterNo)
                .OrderBy(c => c.DisplayOrder)
                .Take(MaxRecommendedCards)
                .ToList();

            foreach (var item in currentBlockCandidates)
            {
                cards.Add(new CourseCardVM
                {
                    CourseId = item.Course.CourseId,
                    CourseName = item.Course.CourseName,
                    Hours = item.Course.Hours > 0 ? item.Course.Hours : 3,
                    Status = "Recommended",
                    CanTake = true
                });
            }

            bool canPullFromNextBlock =
                cards.Count <= MoveToNextBlockWhenCountIs &&
                currentSemesterNo <= LastSemesterAllowedToPullFromNext;

            if (canPullFromNextBlock)
            {
                var nextBlockCandidates = candidates
                    .Where(c => c.SemesterNo > currentSemesterNo)
                    .OrderBy(c => c.SemesterNo)
                    .ThenBy(c => c.DisplayOrder)
                    .ToList();

                foreach (var item in nextBlockCandidates)
                {
                    if (cards.Count == MaxRecommendedCards)
                        break;

                    var courseId = NormalizeCourseId(item.Course.CourseId);

                    if (cards.Any(c => NormalizeCourseId(c.CourseId) == courseId))
                        continue;

                    cards.Add(new CourseCardVM
                    {
                        CourseId = item.Course.CourseId,
                        CourseName = item.Course.CourseName,
                        Hours = item.Course.Hours > 0 ? item.Course.Hours : 3,
                        Status = "Recommended",
                        CanTake = true
                    });
                }
            }

            return cards;
        }
        [HttpPost]
        public async Task<IActionResult> SaveManualElectiveCourses(
    string freeElectiveCourse1,
    string freeElectiveCourse2,
    string freeElectiveCourse3)
        {
            int? studentId = GetCurrentStudentId();

            if (!studentId.HasValue)
                return RedirectToAction("Login", "Account");

            // نحفظها في السيشن عشان تظل ظاهرة في صفحة Course Recommendation
            HttpContext.Session.SetString("FreeElectiveCourse1", freeElectiveCourse1?.Trim() ?? "");
            HttpContext.Session.SetString("FreeElectiveCourse2", freeElectiveCourse2?.Trim() ?? "");
            HttpContext.Session.SetString("FreeElectiveCourse3", freeElectiveCourse3?.Trim() ?? "");

            // ندمج المواد المختارة من الكروت + المواد اليدوية
            var selectedCourses = await GetSelectedCoursesWithManualCoursesAsync(
                studentId.Value,
                freeElectiveCourse1,
                freeElectiveCourse2,
                freeElectiveCourse3
            );

            // نحفظها داخل Form 2
            await SaveSelectedCoursesToForm2Async(studentId.Value, selectedCourses, "Draft");

            

            TempData["Success"] = "Manual elective courses saved.";
            return RedirectToAction(nameof(CourseRecommendation));
        }
        private async Task<List<SelectedCourseVM>> GetSelectedCoursesWithManualCoursesAsync(
    int studentId,
    string? freeElectiveCourse1 = null,
    string? freeElectiveCourse2 = null,
    string? freeElectiveCourse3 = null)
        {
            var selectedCourses = await GetSelectedCoursesFromForm2Async(studentId);

            var manualCourses = await BuildManualElectiveCoursesAsync(
                freeElectiveCourse1 ?? HttpContext.Session.GetString("FreeElectiveCourse1"),
                freeElectiveCourse2 ?? HttpContext.Session.GetString("FreeElectiveCourse2"),
                freeElectiveCourse3 ?? HttpContext.Session.GetString("FreeElectiveCourse3")
            );

            selectedCourses = selectedCourses
                .Where(c => !manualCourses.Any(m =>
                    NormalizeCourseId(m.CourseId) == NormalizeCourseId(c.CourseId)))
                .ToList();

            return selectedCourses
                .Concat(manualCourses)
                .Where(c => !string.IsNullOrWhiteSpace(c.CourseId))
                .GroupBy(c => NormalizeCourseId(c.CourseId))
                .Select(g => g.OrderByDescending(c => c.Hours).First())
                .ToList();
        }
        private async Task<List<SelectedCourseVM>> BuildManualElectiveCoursesAsync(
    string? freeElectiveCourse1,
    string? freeElectiveCourse2,
    string? freeElectiveCourse3)
        {
            var manualInputs = new List<(string? CourseId, string Category)>
{
    (freeElectiveCourse1, "Free/Elective"),
    (freeElectiveCourse2, "Free/Elective"),
    (freeElectiveCourse3, "Free/Elective")
};

            var result = new List<SelectedCourseVM>();

            foreach (var input in manualInputs)
            {
                var normalizedId = NormalizeCourseId(input.CourseId);

                if (string.IsNullOrWhiteSpace(normalizedId))
                    continue;

                if (result.Any(c => NormalizeCourseId(c.CourseId) == normalizedId))
                    continue;

                var existingCourse = await _db.Courses
                    .FirstOrDefaultAsync(c =>
                        c.CourseId.Trim()
                            .ToUpper()
                            .Replace(" ", "")
                            .Replace("-", "")
                            .Replace("_", "")
                            .Replace("/", "") == normalizedId);

                if (existingCourse == null)
                {
                    existingCourse = new Db.Course
                    {
                        CourseId = normalizedId,
                        CourseName = normalizedId,
                        Hours = 3,
                        Prerequisite = null,
                        RequirementCategory = input.Category
                    };

                    _db.Courses.Add(existingCourse);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    if (existingCourse.Hours <= 0)
                        existingCourse.Hours = 3;

                    if (string.IsNullOrWhiteSpace(existingCourse.RequirementCategory))
                        existingCourse.RequirementCategory = input.Category;

                    await _db.SaveChangesAsync();
                }

                result.Add(new SelectedCourseVM
                {
                    CourseId = existingCourse.CourseId,
                    Hours = existingCourse.Hours
                });
            }

            return result;
        }
        private async Task SaveSelectedCoursesToForm2Async(
     int studentId,
     List<SelectedCourseVM> selectedCourses,
     string status)
        {
            var student = await _db.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
                return;

            int advisorId = student.AdvisorId ?? 1;

            var latestForm = await _db.Forms
                .Include(f => f.CourseChoiceMonitoringForm)
                .Where(f => f.StudentId == studentId && f.FormType == "Form 2")
                .OrderByDescending(f => f.FormDate)
                .ThenByDescending(f => f.FormId)
                .FirstOrDefaultAsync();

            if (latestForm == null)
            {
                latestForm = new Acadify.Models.Db.Form
                {
                    StudentId = studentId,
                    AdvisorId = advisorId,
                    FormType = "Form 2",
                    FormDate = DateTime.Now,
                    FormStatus = status,
                    AutoFilled = true,
                    AdvisorConfirmation = status.Contains("Sent")
                };

                _db.Forms.Add(latestForm);
                await _db.SaveChangesAsync();
            }

            if (latestForm.CourseChoiceMonitoringForm == null)
            {
                latestForm.CourseChoiceMonitoringForm = new Acadify.Models.Db.CourseChoiceMonitoringForm
                {
                    FormId = latestForm.FormId
                };

                _db.CourseChoiceMonitoringForms.Add(latestForm.CourseChoiceMonitoringForm);
            }

            latestForm.FormDate = DateTime.Now;
            latestForm.FormStatus = status;
            latestForm.AutoFilled = true;
            latestForm.AdvisorConfirmation = status.Contains("Sent");

            latestForm.CourseChoiceMonitoringForm.SelectedCoursesJson =
                JsonSerializer.Serialize(selectedCourses ?? new List<SelectedCourseVM>());

            latestForm.CourseChoiceMonitoringForm.AdvisedCreditHours =
                selectedCourses?.Sum(c => c.Hours) ?? 0;

            await _db.SaveChangesAsync();
        }

        private async Task<List<SelectedCourseVM>> GetSelectedCoursesFromForm2Async(int studentId)
        {
            var latestForm = await _db.Forms
                .Include(f => f.CourseChoiceMonitoringForm)
                .Where(f => f.StudentId == studentId && f.FormType == "Form 2")
                .OrderByDescending(f => f.FormDate)
                .ThenByDescending(f => f.FormId)
                .FirstOrDefaultAsync();

            var json = latestForm?.CourseChoiceMonitoringForm?.SelectedCoursesJson;

            if (string.IsNullOrWhiteSpace(json))
                return new List<SelectedCourseVM>();

            try
            {
                return JsonSerializer.Deserialize<List<SelectedCourseVM>>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                ) ?? new List<SelectedCourseVM>();
            }
            catch
            {
                return new List<SelectedCourseVM>();
            }
        }





        private string ExtractLastAcademicTerm(string? extractedInfo)
        {
            if (string.IsNullOrWhiteSpace(extractedInfo))
                return "";

            var matches = Regex.Matches(
                extractedInfo,
                @"\b(FALL|SPRING|SUMMER|WINTER)\s+\d{4}/\d{4}\b",
                RegexOptions.IgnoreCase);

            if (matches.Count == 0)
                return "";

            return matches[matches.Count - 1].Value.ToUpperInvariant();
        }

        private string GetNextSemester(string currentSemester)
        {
            if (string.IsNullOrWhiteSpace(currentSemester))
                return "";

            if (currentSemester.Contains("FALL", StringComparison.OrdinalIgnoreCase))
                return "SPRING";

            if (currentSemester.Contains("SPRING", StringComparison.OrdinalIgnoreCase))
                return "SUMMER";

            if (currentSemester.Contains("SUMMER", StringComparison.OrdinalIgnoreCase))
                return "FALL";

            return "";
        }

        private string CalculateLevelFromHours(int earnedHours)
        {
            if (earnedHours <= 0) return "";
            if (earnedHours < 35) return "Level 1-2";
            if (earnedHours < 70) return "Level 3-4";
            if (earnedHours < 105) return "Level 5-6";
            return "Level 7-8";
        }

        private string ExtractDropSubjects(string extractedInfo)
        {
            if (string.IsNullOrWhiteSpace(extractedInfo))
                return "";

            var lines = extractedInfo
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .ToList();

            var dropSubjects = lines
                .Where(l =>
                    l.Contains("drop", StringComparison.OrdinalIgnoreCase) ||
                    l.Contains("withdraw", StringComparison.OrdinalIgnoreCase))
                .ToList();

            return dropSubjects.Any() ? string.Join(", ", dropSubjects) : "";
        }

        private string ExtractICSubjects(string extractedInfo)
        {
            if (string.IsNullOrWhiteSpace(extractedInfo))
                return "";

            var lines = extractedInfo
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .ToList();

            var icSubjects = lines
                .Where(l => l.Contains("IC", StringComparison.OrdinalIgnoreCase))
                .ToList();

            return icSubjects.Any() ? string.Join(", ", icSubjects) : "";
        }

        private string ExtractIPSubjects(string extractedInfo)
        {
            if (string.IsNullOrWhiteSpace(extractedInfo))
                return "";

            var lines = extractedInfo
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .ToList();

            var ipSubjects = lines
                .Where(l => l.Contains("IP", StringComparison.OrdinalIgnoreCase))
                .ToList();

            return ipSubjects.Any() ? string.Join(", ", ipSubjects) : "";
        }
        private static List<TranscriptCourseItem> MergeTranscriptCourses(
     List<TranscriptCourseItem> parserCourses,
     List<TranscriptCourseItem> aiCourses,
     HashSet<string> validCourseIdSet)
        { // قائمة موحدة لتجميع مواد الـ parser والـ AI
            var allCourses = new List<TranscriptCourseItem>();

            // إضافة مواد الـ parser إذا كانت موجودة
            if (parserCourses != null)
                allCourses.AddRange(parserCourses);

            // إضافة مواد الـ AI إذا كانت موجودة
            if (aiCourses != null)
                allCourses.AddRange(aiCourses);

            return allCourses
                // استبعاد أي مادة بدون CourseId
                .Where(c => !string.IsNullOrWhiteSpace(c.CourseId))

                // توحيد كود المادة والقريد قبل المقارنة
                .Select(c =>
                {
                    // توحيد كود المادة مثل CPIS-210 إلى CPIS210
                    c.CourseId = NormalizeCourseId(c.CourseId);

                    // توحيد القريد مثل " np " إلى "NP"
                    c.Grade = NormalizeGrade(c.Grade);

                    // إذا القريد ناجح، نعتبر المادة مجتازة حتى لو IsPassed كانت false
                    c.IsPassed = c.IsPassed || IsPassingGrade(c.Grade);

                    return c;
                })

                // الاحتفاظ فقط بالمواد الموجودة في الخطة الدراسية
                .Where(c => validCourseIdSet.Contains(c.CourseId))

                // إزالة التكرار، ولو نفس المادة ظهرت أكثر من مرة نختار النسخة المجتازة
                .GroupBy(c => c.CourseId)
                .Select(g => g
                    .OrderByDescending(c => c.IsPassed)
                    .First())

                // تحويل النتيجة النهائية إلى List
                .ToList();

        }

        private static string NormalizeCourseId(string? courseId)
        { // توحيد كود المادة بإزالة الرموز والمسافات وتحويله لحروف كبيرة
          // مثال: CPIS-210 / CPIS 210 / cpis_210 تصبح CPIS210
            return (courseId ?? "")
                .Replace("-", "")
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("/", "")
                .Trim()
                .ToUpper();
        }

        private static string NormalizeGrade(string? grade)
        {// توحيد القريد قبل المقارنة
         // مثال: " np " أو "n p" تصبح NP
            return (grade ?? "")
                .Trim()
                .Replace(" ", "")
                .ToUpper();
        }

        private static bool IsPassingGrade(string? grade)
        {
            // الدرجات التي تعتبر ناجحة / منجزة في النظام
            // NP و TR تعتبر ناجحة حسب متطلباتك
            var normalized = NormalizeGrade(grade);

            return normalized == "A+" ||
                   normalized == "A" ||
                   normalized == "B+" ||
                   normalized == "B" ||
                   normalized == "C+" ||
                   normalized == "C" ||
                   normalized == "D+" ||
                   normalized == "D" ||
                   normalized == "P" ||
                   normalized == "NP" ||
                   normalized == "TR";
        }
























        // =========================
        // Form 5 helpers
        // =========================
        private static string NormalizeForm5CourseCode(string? courseId)
        {
            if (string.IsNullOrWhiteSpace(courseId))
                return string.Empty;

            return courseId
                .Trim()
                .ToUpperInvariant()
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("_", "");
        }

        private static string BuildForm5Snapshot(
            bool cpis351,
            bool cpis358,
            bool cpis323,
            bool cpis380,
            bool cpis357,
            bool cpis342)
        {
            return string.Join("; ",
                $"CPIS351={(cpis351 ? 1 : 0)}",
                $"CPIS358={(cpis358 ? 1 : 0)}",
                $"CPIS323={(cpis323 ? 1 : 0)}",
                $"CPIS380={(cpis380 ? 1 : 0)}",
                $"CPIS357={(cpis357 ? 1 : 0)}",
                $"CPIS342={(cpis342 ? 1 : 0)}"
            );
        }

        private async Task CreateOrUpdateForm5AfterTranscriptUploadAsync(
            Db.Student student,
            HashSet<string> courseCodesSet)
        {
            if (!student.AdvisorId.HasValue || student.AdvisorId.Value <= 0)
                return;

            var completedCourses = courseCodesSet
                .Select(NormalizeForm5CourseCode)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            bool cpis351 = completedCourses.Contains("CPIS351");
            bool cpis358 = completedCourses.Contains("CPIS358");
            bool cpis323 = completedCourses.Contains("CPIS323");
            bool cpis380 = completedCourses.Contains("CPIS380");
            bool cpis357 = completedCourses.Contains("CPIS357");
            bool cpis342 = completedCourses.Contains("CPIS342");

            bool isEligible =
                cpis351 &&
                cpis358 &&
                cpis323 &&
                cpis380 &&
                cpis357 &&
                cpis342;

            string snapshot = BuildForm5Snapshot(
                cpis351,
                cpis358,
                cpis323,
                cpis380,
                cpis357,
                cpis342);

            var form = await _db.Forms
                .Include(f => f.GraduationProjectEligibilityForm)
                .Where(f => f.StudentId == student.StudentId && f.FormType == "Form 5")
                .OrderByDescending(f => f.FormDate)
                .ThenByDescending(f => f.FormId)
                .FirstOrDefaultAsync();

            if (form == null)
            {
                form = new Db.Form
                {
                    StudentId = student.StudentId,
                    AdvisorId = student.AdvisorId.Value,
                    FormType = "Form 5",
                    FormDate = DateTime.Now,
                    FormStatus = "Pending",
                    AdvisorNotes = null,
                    AutoFilled = true,
                    AdvisorConfirmation = null
                };

                _db.Forms.Add(form);
                await _db.SaveChangesAsync();
            }
            else
            {
                form.AdvisorId = student.AdvisorId.Value;
                form.FormDate = DateTime.Now;
                form.AutoFilled = true;

                if (string.IsNullOrWhiteSpace(form.FormStatus))
                    form.FormStatus = "Pending";
            }

            var form5 = form.GraduationProjectEligibilityForm;

            if (form5 == null)
            {
                form5 = new Db.GraduationProjectEligibilityForm
                {
                    FormId = form.FormId
                };

                _db.GraduationProjectEligibilityForms.Add(form5);
            }

            form5.RequiredCoursesStatus = snapshot;
            form5.Eligibility = isEligible ? "Eligible" : "Not Eligible";

            await _db.SaveChangesAsync();
        }

        private async Task<int> CreateNewForm5ForStudentAsync(int studentId)
        {
            var advisorId = await GetAdvisorIdForStudentAsync(studentId);

            if (!advisorId.HasValue || advisorId.Value <= 0)
                throw new InvalidOperationException("No advisor is assigned to this student.");

            var newForm5 = new Db.Form
            {
                StudentId = studentId,
                AdvisorId = advisorId.Value,
                FormType = "Form 5",
                FormDate = DateTime.Now,
                FormStatus = "Pending",
                AdvisorNotes = null,
                AutoFilled = true,
                AdvisorConfirmation = null
            };

            _db.Forms.Add(newForm5);
            await _db.SaveChangesAsync();

            var details = new Db.GraduationProjectEligibilityForm
            {
                FormId = newForm5.FormId,
                Eligibility = null,
                RequiredCoursesStatus = null
            };

            _db.GraduationProjectEligibilityForms.Add(details);
            await _db.SaveChangesAsync();

            return newForm5.FormId;
        }
    }

}