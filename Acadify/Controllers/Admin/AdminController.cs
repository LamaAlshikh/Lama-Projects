using Acadify.Models.AdminPages;
using Acadify.Models.Db;
using Acadify.Services.AcademicCalendar.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Db = Acadify.Models.Db;
using System.Collections.Generic;
using System.Globalization;

namespace Acadify.Controllers.Admin
{
    public class AdminController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly Db.AcadifyDbContext _db;
        private readonly IAcademicCalendarAiExtractor _ai;
        private readonly IConfiguration _configuration;

        public AdminController(
            IWebHostEnvironment env,
            Db.AcadifyDbContext db,
            IAcademicCalendarAiExtractor ai,
            IConfiguration configuration)
        {
            _env = env;
            _db = db;
            _ai = ai;
            _configuration = configuration;
        }

        #region Helpers - دوال مساعدة

        private async Task AddNotificationAsync(
            string senderRole,
            string sourceType,
            string type,
            string message,
            int? studentId = null,
            int? advisorId = null,
            int? adminId = null)
        {
            _db.Notifications.Add(new Db.Notification
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
            });

            await _db.SaveChangesAsync();

            var recipientEmail = await GetRecipientEmailAsync(studentId, advisorId, adminId);

            await SendNotificationEmailAsync(
                recipientEmail,
                $"Acadify Notification - {BuildEmailTitle(sourceType)}",
                message);
        }


        private async Task<string?> GetRecipientEmailAsync(int? studentId, int? advisorId, int? adminId)
        {
            if (studentId.HasValue)
            {
                return await _db.Students
                    .Where(s => s.StudentId == studentId.Value)
                    .Select(s => s.User.Email)
                    .FirstOrDefaultAsync();
            }

            if (advisorId.HasValue)
            {
                return await _db.Advisors
                    .Where(a => a.AdvisorId == advisorId.Value)
                    .Select(a => a.User.Email)
                    .FirstOrDefaultAsync();
            }

            if (adminId.HasValue)
            {
                return await _db.Admins
                    .Where(a => a.AdminId == adminId.Value)
                    .Select(a => a.User.Email)
                    .FirstOrDefaultAsync();
            }

            return null;
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
                // Email failure should not stop the main action.
            }
        }

        private static string BuildEmailTitle(string? sourceType)
        {
            return sourceType switch
            {
                "StudentAssigned" => "Student Assigned",
                "Request" => "Student Request",
                "Calendar" => "Academic Calendar",
                _ => "Notification"
            };
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("UserRole") == "Admin";
        }

        #endregion

        // الصفحة الرئيسية للمسؤول
        [HttpGet]
        public IActionResult Index()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            return View();
        }

        #region Manage Advisor Requests - إدارة طلبات المرشدين

        [HttpGet]
        public async Task<IActionResult> ManageAdvisorRequests()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var requests = await _db.AdvisorRequests
                .Include(r => r.Student)
                    .ThenInclude(s => s.User)
                .Include(r => r.RequestedAdvisor)
                    .ThenInclude(a => a!.User)
                .Where(r =>
                    r.Status == "Pending" ||
                    r.Status == "Updated" ||
                    r.Status == "Approved")
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.Advisors = await _db.Advisors
                .Include(a => a.User)
                .OrderBy(a => a.User.Name)
                .ToListAsync();

            var vm = new ManageRequestsVM
            {
                PendingRequests = requests.Select(r => new ManageRequestsVM.RequestRow
                {
                    RequestId = r.RequestId,
                    StudentId = r.StudentId,
                    RequestedAdvisorId = r.RequestedAdvisorId,
                    StudentName = r.Student.Name,
                    UniversityId = r.Student.User.Email,
                    RequestedAdvisorName = r.RequestedAdvisor?.User.Name
                        ?? (!string.IsNullOrWhiteSpace(r.RequestedAdvisorEmail)
                            ? r.RequestedAdvisorEmail
                            : "Not registered yet"),
                    RequestedAdvisorEmail = r.RequestedAdvisor?.User.Email ?? r.RequestedAdvisorEmail ?? "",
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAdvisorRequest(int requestId)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var request = await _db.AdvisorRequests
                .Include(r => r.Student)
                .FirstOrDefaultAsync(r => r.RequestId == requestId);

            if (request == null)
                return NotFound();

            var advisor = await _db.Advisors
                .Include(a => a.User)
                .FirstOrDefaultAsync(a =>
                    a.AdvisorId == request.RequestedAdvisorId ||
                    a.User.Email == request.RequestedAdvisorEmail);

            if (advisor == null)
            {
                TempData["RequestError"] = "The requested advisor is not registered in the system yet.";
                return RedirectToAction(nameof(ManageAdvisorRequests));
            }

            request.Student.AdvisorId = advisor.AdvisorId;
            request.RequestedAdvisorId = advisor.AdvisorId;
            request.RequestedAdvisorEmail = advisor.User.Email;
            request.Status = "Approved";
            request.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            await AddNotificationAsync(
                senderRole: "Admin",
                sourceType: "StudentAssigned",
                type: "student-assigned",
                message: $"A new student has been assigned to you: {request.Student.Name}.",
                advisorId: advisor.AdvisorId);

            TempData["RequestSuccess"] = "The advisor request has been approved and assigned successfully.";
            return RedirectToAction(nameof(ManageAdvisorRequests));
        }
        #region Sent Forms - النماذج المرسلة للجنة الإرشاد

        [HttpGet]
        public async Task<IActionResult> SentForms()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var sentForms = await _db.Forms
                .Include(f => f.Advisor)
                    .ThenInclude(a => a.User)
                .Include(f => f.Student)
                    .ThenInclude(s => s.User)
                .Where(f =>
                    f.FormStatus != null &&
                    (
                        f.FormStatus == "Sent" ||
                        f.FormStatus == "Sent to Advising Committee" ||
                        f.FormStatus == "Submitted" ||
                        f.FormStatus == "Submitted to Advising Committee" ||
                        f.FormStatus.Contains("Sent")
                    ))
                .OrderBy(f => f.Advisor.User.Name)
                .ThenByDescending(f => f.FormDate)
                .ToListAsync();

            var rows = sentForms.Select(f =>
            {
                string formType = NormalizeFormType(f.FormType);
                string viewUrl = BuildSentFormViewUrl(f.FormId, formType, f.StudentId);

                return new AdvisorSentFormRowVM
                {
                    FormId = f.FormId,
                    FormType = formType,
                    FormTitle = GetFormTitle(formType),
                    FormStatus = f.FormStatus ?? "",
                    StudentId = f.StudentId,
                    StudentName = f.Student?.User?.Name ?? f.Student?.Name ?? "Unknown Student",
                    SentDate = f.FormDate,
                    SentDateText = f.FormDate.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture),
                    SentTimeText = f.FormDate.ToString("hh:mm tt", CultureInfo.InvariantCulture),
                    ViewUrl = viewUrl,
                    PrintUrl = viewUrl
                };
            }).ToList();

            var model = new AdvisorSentFormsVM
            {
                TotalForms = rows.Count,
                AdvisorGroups = rows
                    .GroupBy(r =>
                    {
                        var form = sentForms.FirstOrDefault(f => f.FormId == r.FormId);

                        return new
                        {
                            AdvisorId = form?.AdvisorId,
                            AdvisorName = form?.Advisor?.User?.Name ?? "Unknown Advisor",
                            AdvisorEmail = form?.Advisor?.User?.Email ?? ""
                        };
                    })
                    .Select(g => new AdvisorSentFormsGroupVM
                    {
                        AdvisorId = g.Key.AdvisorId,
                        AdvisorName = g.Key.AdvisorName,
                        AdvisorEmail = g.Key.AdvisorEmail,
                        TotalForms = g.Count(),
                        Forms = g.OrderByDescending(x => x.SentDate).ToList()
                    })
                    .OrderBy(g => g.AdvisorName)
                    .ToList()
            };

            return View(model);
        }

        private string BuildSentFormViewUrl(int formId, string formType, int? studentId)
        {
            return formType switch
            {
                "Form 1" => Url.Action("Form1", "Advisor", new { studentId = studentId }) ?? "#",
                "Form 2" => Url.Action("Form2", "Advisor", new { studentId = studentId }) ?? "#",
                "Form 3" => Url.Action("ViewSavedForm3", "Advisor", new { formId = formId }) ?? "#",
                "Form 4" => Url.Action("ViewSavedForm4", "Advisor", new { formId = formId }) ?? "#",
                "Form 5" => Url.Action("Form5", "GraduationProjectEligibility", new { formId = formId }) ?? "#",
                _ => "#"
            };
        }

        private static string NormalizeFormType(string? formType)
        {
            if (string.IsNullOrWhiteSpace(formType))
                return "";

            var value = formType.Trim();

            return value switch
            {
                "Form1" => "Form 1",
                "Form2" => "Form 2",
                "Form3" => "Form 3",
                "Form4" => "Form 4",
                "Form5" => "Form 5",
                _ => value
            };
        }

        private static string GetFormTitle(string formType)
        {
            return formType switch
            {
                "Form 1" => "Academic Advising Confirmation",
                "Form 2" => "Next Semester Course Selection",
                "Form 3" => "Meeting Record Form",
                "Form 4" => "Study Plan Matching",
                "Form 5" => "Graduation Project Eligibility",
                _ => "Academic Form"
            };
        }

        #endregion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectAdvisorRequest(int requestId)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var request = await _db.AdvisorRequests
                .Include(r => r.Student)
                .FirstOrDefaultAsync(r => r.RequestId == requestId);

            if (request == null)
                return NotFound();

            request.Status = "Rejected";
            request.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            TempData["RequestSuccess"] = "The advisor request has been rejected successfully.";
            return RedirectToAction(nameof(ManageAdvisorRequests));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAdvisorRequestInline(int requestId, int? advisorId, string? manualAdvisorEmail)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var request = await _db.AdvisorRequests
                .Include(r => r.Student)
                .FirstOrDefaultAsync(r => r.RequestId == requestId);

            if (request == null)
                return NotFound();

            if (advisorId.HasValue)
            {
                var advisor = await _db.Advisors
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.AdvisorId == advisorId.Value);

                if (advisor == null)
                {
                    TempData["RequestError"] = "The selected advisor was not found.";
                    return RedirectToAction(nameof(ManageAdvisorRequests));
                }

                request.RequestedAdvisorId = advisor.AdvisorId;
                request.RequestedAdvisorEmail = advisor.User.Email;

                // إذا الطلب Approved، نحدث المرشد الفعلي للطالبة
                request.Student.AdvisorId = advisor.AdvisorId;
            }
            else if (!string.IsNullOrWhiteSpace(manualAdvisorEmail))
            {
                var email = manualAdvisorEmail.Trim();

                var advisor = await _db.Advisors
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.User.Email == email);

                if (advisor != null)
                {
                    request.RequestedAdvisorId = advisor.AdvisorId;
                    request.RequestedAdvisorEmail = advisor.User.Email;

                    // إذا الإيميل تابع لمرشد موجود، نحدث المرشد الفعلي للطالبة
                    request.Student.AdvisorId = advisor.AdvisorId;
                }
                else
                {
                    request.RequestedAdvisorId = null;
                    request.RequestedAdvisorEmail = email;
                }
            }
            else
            {
                TempData["RequestError"] = "Please select an advisor or enter the advisor email.";
                return RedirectToAction(nameof(ManageAdvisorRequests));
            }

            if (request.Status != "Approved")
            {
                request.Status = "Updated";
            }

            request.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            TempData["RequestSuccess"] = "The advisor request has been updated successfully.";
            return RedirectToAction(nameof(ManageAdvisorRequests));
        }

        #endregion

        #region Academic Calendar - التقويم الأكاديمي

        [HttpGet]
        public IActionResult UploadAcademicCalendar()
        {
            return View(new UploadAcademicCalendarModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAcademicCalendar(UploadAcademicCalendarModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Message = "Please check the required fields.";
                model.IsSuccess = false;
                return View(model);
            }

            if (model.AcademicCalendarFile == null || model.AcademicCalendarFile.Length == 0)
            {
                model.Message = "Please choose a xlsx file first.";
                model.IsSuccess = false;
                return View(model);
            }

            var ext = Path.GetExtension(model.AcademicCalendarFile.FileName).ToLower();

            if (ext != ".xlsx")
            {
                model.Message = "Only Excel (.xlsx) files are allowed.";
                model.IsSuccess = false;
                return View(model);
            }

            try
            {
                var folder = Path.Combine(_env.WebRootPath, "uploads", "academic-calendar");
                Directory.CreateDirectory(folder);

                var savedFileName = $"{Guid.NewGuid():N}.xlsx";
                var savedPath = Path.Combine(folder, savedFileName);

                using (var stream = new FileStream(savedPath, FileMode.Create))
                {
                    await model.AcademicCalendarFile.CopyToAsync(stream);
                }

                var calendar = new AcademicCalendar
                {
                    PdfFile = savedFileName,
                    UploadedAt = DateTime.Now
                };

                _db.AcademicCalendars.Add(calendar);
                await _db.SaveChangesAsync();

                var events = await _ai.ExtractEventsFromPdfAsync(savedPath, calendar.CalendarId);

                if (events != null && events.Count > 0)
                {
                    _db.AcademicCalendarEvents.AddRange(events);
                    await _db.SaveChangesAsync();
                }

                model.Message = "Academic calendar uploaded successfully.";
                model.IsSuccess = true;
            }
            catch
            {
                model.Message = "An error occurred while uploading the academic calendar.";
                model.IsSuccess = false;
            }

            return View(model);
        }

        #endregion
    }
}