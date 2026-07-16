using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WepApp2.Models;
using WepApp2.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace WepApp2.Controllers
{
    // [Authorize(Roles = "مدير,Admin")]  // مؤقتاً معطل للاختبار
    public class ReportsController : Controller
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Reports/AllReports
        public IActionResult AllReports()
        {
            try
            {
                // حساب تاريخ بداية الشهر الحالي
                var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                // عدد الطلبات هذا الشهر
                var requestsThisMonth = _context.Requests
                    .Where(r => r.RequestDate >= startOfMonth && r.RequestDate <= endOfMonth)
                    .Count();

                // عدد الأجهزة التي تحتاج صيانة
                var devicesNeedMaintenance = _context.Devices
                    .Where(d => d.DeviceStatus == "صيانة" || d.DeviceStatus == "Maintenance" ||
                               d.DeviceStatus == "maintenance" || d.DeviceStatus == "تحت الصيانة")
                    .Count();

                // توزيع الأجهزة حسب الحالة
                var deviceStatusData = _context.Devices
                    .GroupBy(d => d.DeviceStatus ?? "غير محدد")
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToList();

                // أنواع الطلبات خلال الشهر
                var requestTypesData = _context.Requests
                    .Where(r => r.RequestDate >= startOfMonth && r.RequestDate <= endOfMonth)
                    .GroupBy(r => r.RequestType ?? "غير محدد")
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToList();

                // توزيع المستخدمين حسب النوع (باستثناء المدير)
                var usersDistributionData = _context.Users
                    .Where(u => u.UserRole != "مدير"&&u.UserRole != "مشرف")
                    .GroupBy(u => u.UserRole ?? "غير محدد")
                    .Select(g => new { UserType = g.Key, Count = g.Count() })
                    .ToList();

                // توزيع الأجهزة حسب النوع
                var deviceTypesData = _context.Devices
                    .GroupBy(d => d.DeviceName ?? "غير محدد")
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToList();

                // تمرير البيانات إلى View
                ViewBag.RequestsThisMonth = requestsThisMonth;
                ViewBag.DevicesNeedMaintenance = devicesNeedMaintenance;
                ViewBag.DeviceStatusData = deviceStatusData;
                ViewBag.RequestTypesData = requestTypesData;
                ViewBag.UsersDistributionData = usersDistributionData;
                ViewBag.DeviceTypesData = deviceTypesData;

                return View();
            }
            catch (Exception ex)
            {
                // في حالة حدوث خطأ، إرسال بيانات افتراضية
                ViewBag.RequestsThisMonth = 0;
                ViewBag.DevicesNeedMaintenance = 0;
                ViewBag.DeviceStatusData = new List<object>();
                ViewBag.RequestTypesData = new List<object>();
                ViewBag.UsersDistributionData = new List<object>();
                ViewBag.DeviceTypesData = new List<object>();

                // يمكنك تسجيل الخطأ هنا إذا كان لديك نظام تسجيل
                // _logger.LogError(ex, "Error in AllReports");

                return View();
            }
        }

        // GET: Reports/CreateCustomReport
        public IActionResult CreateCustomReport()
        {
            return View();
        }

        // POST: Reports/CreateCustomReport
        [HttpPost]
        public IActionResult CreateCustomReport(string reportTitle, string reportType, DateTime? fromDate, DateTime? toDate, string requestStatus, string deviceStatus, string userType, string serviceType, List<string> fields)
        {
            try
            {
                // إضافة معلومات إضافية للـ ViewBag
                ViewBag.ServiceType = serviceType;

                if (reportType == "تقرير الطلبات")
                {
                    // جلب البيانات من قاعدة البيانات
                    var requestsQuery = _context.Requests
                        .Include(r => r.User)
                        .Include(r => r.Service)
                        .Include(r => r.Device)
                        .AsQueryable();

                    // تطبيق فلتر التاريخ إذا تم تحديده
                    if (fromDate.HasValue)
                    {
                        requestsQuery = requestsQuery.Where(r => r.RequestDate >= fromDate.Value);
                    }
                    if (toDate.HasValue)
                    {
                        requestsQuery = requestsQuery.Where(r => r.RequestDate <= toDate.Value);
                    }

                    // تطبيق فلتر حالة الطلب إذا تم تحديده
                    if (!string.IsNullOrEmpty(requestStatus))
                    {
                        requestsQuery = requestsQuery.Where(r =>
                            r.AdminStatus == requestStatus ||
                            r.SupervisorStatus == requestStatus);
                    }

                    var requests = requestsQuery.ToList();

                    // جلب جميع المشرفين مرة واحدة لتحسين الأداء
                    var supervisorIds = requests.Select(r => r.SupervisorAssigned).Distinct().ToList();
                    var supervisors = _context.Users
                        .Where(u => supervisorIds.Contains(u.UserID))
                        .ToDictionary(u => u.UserID, u => u.FirstName + " " + u.LastName);

                    // تحويل البيانات إلى كائن ديناميكي للعرض
                    var reportData = requests.Select(r => new
                    {
                        Id = r.RequestID,
                        المستفيد = GetUserFullName(r.User),
                        نوع_الخدمة = GetServiceName(r),
                        الجهاز = r.Device?.DeviceName ?? "لا يوجد",
                        التاريخ = r.RequestDate.ToString("yyyy-MM-dd"),
                        الوقت = r.RequestDate.ToString("HH:mm"),
                        المشرف_المسند = r.SupervisorAssigned.HasValue && supervisors.ContainsKey(r.SupervisorAssigned.Value)
                            ? supervisors[r.SupervisorAssigned.Value]
                            : "غير مسند",
                        الحالة = GetRequestStatus(r)
                    }).ToList();

                    ViewBag.ReportTitle = reportTitle;
                    ViewBag.ReportType = reportType;
                    ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
                    ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
                    ViewBag.SelectedFields = fields ?? new List<string>();

                    return View("PrintReport", reportData);
                }
                else if (reportType == "تقرير الأجهزة")
                {
                    // جلب بيانات الأجهزة من قاعدة البيانات
                    var devicesQuery = _context.Devices.AsQueryable();

                    // تطبيق فلتر حالة الجهاز إذا تم تحديده
                    if (!string.IsNullOrEmpty(deviceStatus))
                    {
                        devicesQuery = devicesQuery.Where(d => d.DeviceStatus == deviceStatus);
                    }

                    var devices = devicesQuery.ToList();

                    // تحويل البيانات إلى كائن ديناميكي - استخدام أسماء عربية لتتوافق مع PrintReport.cshtml
                    var deviceData = devices.Select(d => new
                    {
                        Id = d.DeviceID,
                        اسم_الجهاز = d.DeviceName,
                        الموقع = d.DeviceLocation ?? "غير محدد",
                        الشركة = d.BrandName ?? "غير محدد",
                        الطراز = d.DeviceModel ?? "غير محدد",
                        تاريخ_آخر_صيانة = d.LastMaintenance?.ToString("yyyy-MM-dd") ?? "غير محدد",
                        الحالة = d.DeviceStatus
                    }).ToList();

                    ViewBag.ReportTitle = reportTitle;
                    ViewBag.ReportType = reportType;
                    ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
                    ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
                    ViewBag.SelectedFields = fields ?? new List<string>();

                    return View("PrintReport", deviceData);
                }
                else if (reportType == "تقرير المستخدمين")
                {
                    // جلب بيانات المستخدمين من قاعدة البيانات
                    var usersQuery = _context.Users.AsQueryable();

                    // تطبيق فلتر نوع المستخدم إذا تم تحديده
                    if (!string.IsNullOrEmpty(userType))
                    {
                        usersQuery = usersQuery.Where(u => u.UserRole == userType);
                    }

                    var users = usersQuery.ToList();

                    // تحويل البيانات إلى كائن ديناميكي مع أسماء عربية
                    var userData = users.Select(u => new
                    {
                        Id = u.UserID,
                        الاسم = u.FirstName + " " + u.LastName,
                        اسم_المستخدم = u.UserName,
                        نوع_المستخدم = u.UserRole,
                        الجهة = u.Faculty ?? "غير محدد",
                        القسم = u.Department ?? "غير محدد",
                        البريد_الإلكتروني = u.Email,
                        رقم_الجوال = u.PhoneNumber
                    }).ToList();

                    ViewBag.ReportTitle = reportTitle;
                    ViewBag.ReportType = reportType;
                    ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
                    ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
                    ViewBag.SelectedFields = fields ?? new List<string>();

                    return View("PrintReport", userData);
                }
                else if (reportType == "تقرير الخدمات")
                {
                    if (serviceType == "زيارة المعمل")
                    {
                        // جلب بيانات زيارات المعمل مع تفاصيلها
                        var labVisitsQuery = _context.LabVisits
                            .Include(lv => lv.Request)
                                .ThenInclude(r => r.User)
                            .Include(lv => lv.VisitDetails)
                            .Include(lv => lv.Service)
                            .AsQueryable();

                        // تطبيق فلتر التاريخ
                        if (fromDate.HasValue)
                        {
                            labVisitsQuery = labVisitsQuery.Where(lv => lv.VisitDate >= fromDate.Value);
                        }
                        if (toDate.HasValue)
                        {
                            labVisitsQuery = labVisitsQuery.Where(lv => lv.VisitDate <= toDate.Value);
                        }

                        var labVisits = labVisitsQuery.ToList();

                        // تحويل البيانات للعرض
                        var labVisitData = labVisits.Select(lv => new
                        {
                            نوع_الزيارة = lv.VisitDetails?.VisitType ?? "زيارة عامة",
                            وصف_الزيارة = lv.AdditionalNotes ?? "لا يوجد وصف",
                            اسم_المستفيد = lv.Request?.User != null
                                ? $"{lv.Request.User.FirstName} {lv.Request.User.LastName}".Trim()
                                : lv.PreferredContactMethod ?? "زائر خارجي",
                            تاريخ_الزيارة = lv.VisitDate.ToString("yyyy-MM-dd"),
                            الحالة = lv.Request?.AdminStatus ??
                                    lv.Request?.SupervisorStatus ??
                                    (lv.VisitDetails?.IsDeleted == false ? "نشط" : "جديد"),
                            عدد_الزوار = lv.NumberOfVisitors,
                            الوقت = lv.PreferredTime.ToString(@"hh\:mm")
                        }).ToList();

                        ViewBag.ReportTitle = reportTitle;
                        ViewBag.ReportType = reportType;
                        ViewBag.ServiceType = serviceType;
                        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
                        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
                        ViewBag.SelectedFields = fields ?? new List<string>();

                        return View("PrintReport", labVisitData);
                    }
                    else if (serviceType == "حجز الأجهزة")
                    {
                        // جلب بيانات حجز الأجهزة مع العلاقات
                        var bookingDevicesQuery = _context.BookingDevices
                            .Include(bd => bd.Request)
                                .ThenInclude(r => r.User)
                            .Include(bd => bd.Device) // إذا كانت هناك علاقة مع جدول الأجهزة
                            .AsQueryable();

                        // تطبيق فلتر التاريخ باستخدام BookingDate
                        if (fromDate.HasValue)
                        {
                            var fromDateOnly = DateOnly.FromDateTime(fromDate.Value);
                            bookingDevicesQuery = bookingDevicesQuery.Where(bd => bd.BookingDate >= fromDateOnly);
                        }
                        if (toDate.HasValue)
                        {
                            var toDateOnly = DateOnly.FromDateTime(toDate.Value);
                            bookingDevicesQuery = bookingDevicesQuery.Where(bd => bd.BookingDate <= toDateOnly);
                        }

                        var bookings = bookingDevicesQuery.ToList();

                        // تحويل البيانات للعرض
                        var bookingData = bookings.Select(b => new
                        {
                            // استخدم ProjectName كاسم الجهاز إذا لم تكن هناك علاقة مع جدول الأجهزة
                            اسم_الجهاز = b.Device?.DeviceName ?? b.ProjectName ?? "غير محدد",
                            اسم_المستفيد = b.Request?.User != null
                                ? $"{b.Request.User.FirstName} {b.Request.User.LastName}".Trim()
                                : "غير محدد",
                            اسم_المشروع = b.ProjectName ?? "غير محدد",
                            وصف_المشروع = b.ProjectDescription ?? "لا يوجد وصف",
                            تاريخ_الحجز = b.BookingDate.ToString("yyyy-MM-dd"),
                            بداية_الوقت = b.StartTime.ToString("HH:mm"),
                            نهاية_الوقت = b.EndTime.ToString("HH:mm")
                        }).ToList();

                        ViewBag.ReportTitle = reportTitle;
                        ViewBag.ReportType = reportType;
                        ViewBag.ServiceType = serviceType;
                        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
                        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
                        ViewBag.SelectedFields = fields ?? new List<string>();

                        return View("PrintReport", bookingData);
                    }
                    else if (serviceType == "إعارة الأجهزة")
                    {
                        // جلب بيانات إعارة الأجهزة مع العلاقات
                        var deviceLoansQuery = _context.DeviceLoans.AsQueryable();

                        // تطبيق فلتر التاريخ
                        if (fromDate.HasValue && toDate.HasValue)
                        {
                            var fromDateOnly = DateOnly.FromDateTime(fromDate.Value);
                            var toDateOnly = DateOnly.FromDateTime(toDate.Value);
                            deviceLoansQuery = deviceLoansQuery.Where(dl =>
                                dl.StartDate >= fromDateOnly && dl.EndDate <= toDateOnly);
                        }

                        var loans = deviceLoansQuery.ToList();

                        // جلب معلومات الأجهزة
                        var deviceIds = loans.Where(l => l.DeviceId.HasValue)
                            .Select(l => l.DeviceId.Value).Distinct().ToList();
                        var devices = _context.Devices
                            .Where(d => deviceIds.Contains(d.DeviceID))
                            .ToDictionary(d => d.DeviceID, d => d.DeviceName);

                        // جلب معلومات المستخدمين من خلال الطلبات
                        var requestIds = loans.Where(l => l.RequestId.HasValue)
                            .Select(l => l.RequestId.Value).Distinct().ToList();
                        var requests = _context.Requests
                            .Include(r => r.User)
                            .Where(r => requestIds.Contains(r.RequestID))
                            .ToDictionary(r => r.RequestID);

                        // تحويل البيانات للعرض
                        var loanData = loans.Select(l => {
                            // الحصول على اسم الجهاز
                            string deviceName = "غير محدد";
                            if (l.DeviceId.HasValue && devices.ContainsKey(l.DeviceId.Value))
                            {
                                deviceName = devices[l.DeviceId.Value];
                            }

                            // الحصول على معلومات المستخدم
                            string userName = "مستخدم النظام";
                            string status = "نشط";
                            if (l.RequestId.HasValue && requests.ContainsKey(l.RequestId.Value))
                            {
                                var request = requests[l.RequestId.Value];
                                if (request.User != null)
                                {
                                    userName = $"{request.User.FirstName} {request.User.LastName}".Trim();
                                }
                                status = request.AdminStatus ?? request.SupervisorStatus ?? "نشط";
                            }
                            else if (!string.IsNullOrEmpty(l.PreferredContactMethod))
                            {
                                userName = l.PreferredContactMethod;
                            }

                            return new
                            {
                                اسم_الجهاز = deviceName,
                                الغرض = l.Purpose ?? "غير محدد",
                                مدة_الإعارة = (l.EndDate.ToDateTime(TimeOnly.MinValue) - l.StartDate.ToDateTime(TimeOnly.MinValue)).Days + " يوم",
                                مقدم_الطلب = userName,
                                الحالة = status
                            };
                        }).ToList();

                        ViewBag.ReportTitle = reportTitle;
                        ViewBag.ReportType = reportType;
                        ViewBag.ServiceType = serviceType;
                        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
                        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
                        ViewBag.SelectedFields = fields ?? new List<string>();

                        return View("PrintReport", loanData);
                    }
                    else if (serviceType == "الدورات التدريبية")
                    {
                        // جلب بيانات الدورات التدريبية
                        var coursesQuery = _context.Courses.AsQueryable();

                        // تطبيق فلتر التاريخ إذا لزم الأمر
                        var courses = coursesQuery.Where(c => c.IsDeleted == false).ToList();

                        // تحويل البيانات للعرض
                        var courseData = courses.Select(c => new
                        {
                            نوع_الخدمة = "الدورات التدريبية",
                            اسم_الدورة = c.CourseName ?? "غير محدد",
                            مجال_الدورة = c.CourseField ?? "غير محدد",
                            مقدم_الدورة = c.PresenterName ?? "غير محدد",
                            وصف_الدورة = c.CourseDescription ?? "لا يوجد وصف"
                        }).ToList();

                        ViewBag.ReportTitle = reportTitle;
                        ViewBag.ReportType = reportType;
                        ViewBag.ServiceType = serviceType;
                        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
                        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
                        ViewBag.SelectedFields = fields ?? new List<string>();

                        return View("PrintReport", courseData);
                    }
                    else if (serviceType == "الاستشارات التقنية")
                    {
                        // جلب بيانات الاستشارات التقنية
                        var consultationsQuery = _context.Consultations
                            .Include(c => c.ConsultationMajor)
                            .Include(c => c.Request)
                                .ThenInclude(r => r.User)
                            .AsQueryable();

                        // تطبيق فلتر التاريخ
                        if (fromDate.HasValue)
                        {
                            var fromDateOnly = DateOnly.FromDateTime(fromDate.Value);
                            consultationsQuery = consultationsQuery.Where(c => c.ConsultationDate >= fromDateOnly);
                        }
                        if (toDate.HasValue)
                        {
                            var toDateOnly = DateOnly.FromDateTime(toDate.Value);
                            consultationsQuery = consultationsQuery.Where(c => c.ConsultationDate <= toDateOnly);
                        }

                        var consultations = consultationsQuery.ToList();

                        var supervisorIds = consultations
     .Where(c => c.Request != null && c.Request.SupervisorAssigned.HasValue)
     .Select(c => c.Request.SupervisorAssigned.Value)
     .Distinct()
     .ToList();

                        var supervisors = _context.Users
                            .Where(u => supervisorIds.Contains(u.UserID))
                            .ToDictionary(u => u.UserID, u => $"{u.FirstName} {u.LastName}".Trim());

                        // تحويل البيانات للعرض
                        var consultationData = consultations.Select(c => {
                            // الحصول على اسم المشرف (مقدم الاستشارة)
                            string supervisorName = "غير مسند";
                            if (c.Request != null && c.Request.SupervisorAssigned.HasValue && supervisors.ContainsKey(c.Request.SupervisorAssigned.Value))
                            {
                                supervisorName = supervisors[c.Request.SupervisorAssigned.Value];
                            }

                            return new
                            {
                                عنوان_الاستشارة = c.ConsultationDescription ?? "غير محدد",
                                مجال_الاستشارة = c.ConsultationMajor?.Major ?? "غير محدد",
                                وصف_الاستشارة = c.ConsultationDescription ?? "لا يوجد وصف",
                                المستفيد = c.Request?.User != null
                                    ? $"{c.Request.User.FirstName} {c.Request.User.LastName}".Trim()
                                    : c.PreferredContactMethod ?? "غير محدد",
                                مقدم_الاستشارة = supervisorName, // مقدم الاستشارة من SupervisorAssigned
                                التاريخ = c.ConsultationDate.ToString("yyyy-MM-dd"),
                                الوقت = c.AvailableTimes.ToString("HH:mm"),
                                الحالة = c.Request?.AdminStatus ??
                                        c.Request?.SupervisorStatus ??
                                        "جديد"
                            };
                        }).ToList();

                        ViewBag.ReportTitle = reportTitle;
                        ViewBag.ReportType = reportType;
                        ViewBag.ServiceType = serviceType;
                        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
                        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
                        ViewBag.SelectedFields = fields ?? new List<string>();

                        return View("PrintReport", consultationData);
                    }
                    else
                    {
                        // جميع الخدمات - الكود الحالي
                        var servicesQuery = _context.Requests
                            .Include(r => r.User)
                            .Include(r => r.Service)
                            .Include(r => r.Device)
                            .AsQueryable();

                        // تطبيق فلتر التاريخ
                        if (fromDate.HasValue)
                        {
                            servicesQuery = servicesQuery.Where(r => r.RequestDate >= fromDate.Value);
                        }
                        if (toDate.HasValue)
                        {
                            servicesQuery = servicesQuery.Where(r => r.RequestDate <= toDate.Value);
                        }

                        var services = servicesQuery.ToList();

                        var serviceData = services.Select(r => new
                        {
                            نوع_الخدمة = GetServiceName(r),
                            وصف_الخدمة = r.RequestType ?? "غير محدد",
                            تاريخ_الطلب = r.RequestDate.ToString("yyyy-MM-dd"),
                            المستخدم = GetUserFullName(r.User),
                            الحالة = GetRequestStatus(r)
                        }).ToList();

                        ViewBag.ReportTitle = reportTitle;
                        ViewBag.ReportType = reportType;
                        ViewBag.ServiceType = serviceType;
                        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
                        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
                        ViewBag.SelectedFields = fields ?? new List<string>();

                        return View("PrintReport", serviceData);
                    }
                }
            
                else
                {
                    // للتقارير الأخرى
                    ViewBag.ReportTitle = reportTitle;
                    ViewBag.ReportType = reportType;
                    ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
                    ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

                    return View("PrintReport", new List<object>());
                }
            }
            catch (Exception ex)
            {
                // في حالة حدوث أي خطأ، نعود إلى الصفحة مع رسالة خطأ
                TempData["Error"] = "حدث خطأ في إنشاء التقرير: " + ex.Message;
                return RedirectToAction("CreateCustomReport");
            }
        }

        // دالة مساعدة للحصول على الاسم الكامل للمستخدم
        private string GetUserFullName(User user)
        {
            if (user == null)
                return "غير محدد";

            return $"{user.FirstName} {user.LastName}".Trim();
        }

        // دالة مساعدة للحصول على اسم الخدمة
        private string GetServiceName(Request request)
        {
            if (request.Service != null)
                return request.Service.ServiceName;
            else if (!string.IsNullOrEmpty(request.RequestType))
                return request.RequestType;
            else
                return "غير محدد";
        }

        // دالة مساعدة للحصول على حالة الطلب
        private string GetRequestStatus(Request request)
        {
            // إعطاء الأولوية لحالة الأدمن إذا كانت موجودة
            if (!string.IsNullOrEmpty(request.AdminStatus))
                return request.AdminStatus;
            else if (!string.IsNullOrEmpty(request.SupervisorStatus))
                return request.SupervisorStatus;
            else
                return "جديد";
        }

        [HttpGet]
        public IActionResult PrintReport()
        {
            return View();
        }
    }
}