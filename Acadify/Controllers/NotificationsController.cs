using AcadifyDbContext = Acadify.Models.Db.AcadifyDbContext;
using Notification = Acadify.Models.Db.Notification;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Acadify.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly AcadifyDbContext _db;

        public NotificationsController(AcadifyDbContext db)
        {
            _db = db;
        }

        // عرض لوحة الإشعارات (تستدعي الـ ViewComponent الذي دمجناه سابقاً)
        public IActionResult Panel()
        {
            return ViewComponent("Notifications");
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notif = await _db.Notifications.FindAsync(id);
            if (notif == null) return NotFound();

            notif.IsRead = true;
            await _db.SaveChangesAsync();
            return Ok();
        }

        #region Core Logic - دوال الإرسال الأساسية

        // إرسال إشعار لمستخدم محدد
        public async Task AddNotificationAsync(
            string senderRole, string sourceType, string type, string message,
            int? studentId = null, int? advisorId = null, int? adminId = null)
        {
            _db.Notifications.Add(new Notification
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
        }

        // إرسال إشعار لجميع المشرفين (Admins)
        public async Task AddNotificationToAllAdminsAsync(
            string senderRole, string sourceType, string type, string message,
            int? studentId = null, int? advisorId = null)
        {
            var admins = await _db.Admins.ToListAsync();
            foreach (var admin in admins)
            {
                _db.Notifications.Add(new Notification
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
                });
            }
            await _db.SaveChangesAsync();
        }

        // إرسال للمرشد المسؤول عن الطالب، وإذا لم يوجد مرشد يرسل للأدمن
        public async Task AddNotificationToAdvisorOrAdminsAsync(
            int studentId, string senderRole, string sourceType, string type, string message)
        {
            var student = await _db.Students.FirstOrDefaultAsync(s => s.StudentId == studentId);
            if (student == null) return;

            if (student.AdvisorId.HasValue)
            {
                await AddNotificationAsync(senderRole, sourceType, type, message, studentId, student.AdvisorId.Value);
            }
            else
            {
                await AddNotificationToAllAdminsAsync(senderRole, sourceType, type, message, studentId);
            }
        }
        #endregion

        #region Actions - العمليات المختلفة

        // التوصيات (Recommendations)
        [HttpPost]
        public async Task<IActionResult> RecommendationToStudent(int studentId, string message)
        {
            await AddNotificationAsync("System", "Recommendation", "initial recommendation", message, studentId: studentId);
            return Ok();
        }

        // الاجتماعات (Meetings)
        [HttpPost]
        public async Task<IActionResult> MeetingStudentToAdvisor(int studentId, string message)
        {
            var student = await _db.Students.FindAsync(studentId);
            if (student?.AdvisorId == null) return BadRequest("Advisor not found.");

            await AddNotificationAsync("Student", "Meeting", "meeting request", message, studentId, student.AdvisorId);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> MeetingAdvisorToStudent(int studentId, string message)
        {
            await AddNotificationAsync("Advisor", "Meeting", "meeting response", message, studentId: studentId);
            return Ok();
        }

        // النماذج (Forms)
        [HttpPost]
        public async Task<IActionResult> Form2StudentActionToAdvisor(int studentId, string message)
        {
            var student = await _db.Students.FindAsync(studentId);
            if (student?.AdvisorId == null) return BadRequest("Advisor not found.");

            await AddNotificationAsync("Student", "Form", "form2 action", message, studentId, student.AdvisorId);
            return Ok();
        }

        // السجل الأكاديمي (Transcript)
        [HttpPost]
        public async Task<IActionResult> TranscriptUploadedToAdvisorOrAdmin(int studentId, string message)
        {
            await AddNotificationToAdvisorOrAdminsAsync(studentId, "Student", "Transcript", "transcript uploaded", message);
            return Ok();
        }

        // خطة الدراسة (Study Plan)
        [HttpPost]
        public async Task<IActionResult> StudyPlanUploadedToAdmin(string message)
        {
            await AddNotificationToAllAdminsAsync("System", "StudyPlan", "study plan uploaded", message);
            return Ok();
        }
        #endregion

        // دوال مساعدة لجلب البيانات من الـ Session إذا لزم الأمر في الـ Views
        private int? GetSessionStudentId() => HttpContext.Session.GetInt32("StudentId");
        private int? GetSessionAdvisorId() => HttpContext.Session.GetInt32("AdvisorId");
        private int? GetSessionAdminId() => HttpContext.Session.GetInt32("AdminId");
    }
}