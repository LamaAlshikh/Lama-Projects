// ===========================================================
// الكنترولر: DashboardController
// الوظيفة: عرض لوحة البيانات وتحليل الإحصائيات المتعلقة بالأجهزة والخدمات والصيانة
// Controller: DashboardController
// Purpose: Displays dashboard view and handles statistics for devices, services, and maintenance
// ===========================================================


using WepApp2.Models;
using WepApp2.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace WepApp2.Controllers
{
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }


		// ============================
		// دالة Index: نقطة الدخول الرئيسية للوحة البيانات
		// Index(): Main entry point for the dashboard
		// ============================
		public IActionResult Index(string? role, string period = "monthly")

        {
			// الحصول على الدور المحدد (اختياري) من رابط الطلب
			// Get selected role from the query string
			var selectedRole = HttpContext.Request.Query["role"].ToString();

			//  جلب الطلبات وربطها بالمستخدمين مع فلترة حسب الدور إن وُجد
			// Retrieve requests and join with users, optionally filter by role
			var filteredRequests = _context.Requests
				.Include(r => r.User)
				.Where(r => string.IsNullOrEmpty(selectedRole) || r.User.UserRole == selectedRole)
				.ToList();

			//  حساب إجمالي استخدام الأجهزة
			// Count total device usage (based on BookingDevices)
			var totalDeviceUsage = filteredRequests
				.SelectMany(r => _context.BookingDevices.Where(b => b.RequestId == r.RequestID))
				.Count();

			//  تفضيلات المستخدمين للأجهزة (نسبة استخدام كل جهاز)
			// User device preferences (percentage of use per device)
			var requestIds = filteredRequests.Select(r => r.RequestID).ToList();
            var userPreferences = _context.Devices
      .Select(d => new UserPreferenceDto
      {
          DeviceName = d.DeviceName,
          PreferencePercentage = (int)(totalDeviceUsage == 0
              ? 0
              : Math.Round(
                  ((double)_context.BookingDevices
                      .Where(b => b.DeviceId == d.DeviceID && b.RequestId.HasValue && requestIds.Contains(b.RequestId.Value))
                      .Count()
                  / totalDeviceUsage) * 100, 2))
      }).ToList();


			//  تحليل الذروة الزمنية للاستخدام
			// Analyze time peaks based on selected period (monthly/weekly/yearly)
			List<TimePeakDto> timePeaks = new();

            if (period == "yearly")
            {
                timePeaks = filteredRequests
                    .GroupBy(r => r.RequestDate.Year)
                    .Select(g => new TimePeakDto
                    {
                        TimePeriod = g.Key.ToString(), // مثل "2025"
                        UsageCount = g.Count()
                    })
                    .OrderBy(g => g.TimePeriod)
                    .ToList();
            }
            else if (period == "weekly")
            {
                timePeaks = filteredRequests
                    .GroupBy(r => System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                        r.RequestDate, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Saturday))
                    .Select(g => new TimePeakDto
                    {
                        TimePeriod = $"الأسبوع {g.Key}",
                        UsageCount = g.Count()
                    })
                    .ToList();
            }
            else // default monthly
            {
                timePeaks = filteredRequests
                    .GroupBy(r => new { r.RequestDate.Year, r.RequestDate.Month })
                    .Select(g => new TimePeakDto
                    {
                        TimePeriod = $"{g.Key.Year}-{g.Key.Month:D2}", // مثل "2025-07"
                        UsageCount = g.Count()
                    })
                    .OrderBy(g => g.TimePeriod)
                    .ToList();
            }



			//  فعالية الصيانة (نسبة الإنجاز لكل شهر)
			// Maintenance effectiveness (monthly completion rate)
			var maintenanceRequests = filteredRequests
				.Where(r => r.RequestType == "Maintenance")
				.ToList();

			var monthlyMaintenanceStats = maintenanceRequests
				.GroupBy(r => new { r.RequestDate.Year, r.RequestDate.Month })
				.Select(g => new TimePeakDto
				{
					TimePeriod = $"{g.Key.Year}-{g.Key.Month:D2}",
					UsageCount = g.Count(r => r.AdminStatus == "Completed") * 100 / g.Count()
				})
				.OrderBy(g => g.TimePeriod)
				.ToList();



			// 🔷 استخدام الخدمات (عدد مرات طلب كل خدمة)
			// Service usage counts
			var serviceUsages = _context.Services
           .Select(s => new { s.ServiceID, s.ServiceName })
           .AsEnumerable()
           .Select(s => new ServiceUsageDto
            {
             ServiceName = s.ServiceName,
             UsageCount = filteredRequests.Count(r => r.ServiceId == s.ServiceID)
           }).ToList();




			//  بناء النموذج وإرساله للواجهة
			// Construct and return the dashboard view model
			var model = new DashboardViewModel
			{
				ServiceUsages = serviceUsages,
				TimePeaks = timePeaks,
				MaintenancePeaks = monthlyMaintenanceStats,
				UserPreferences = userPreferences,

				DeviceAvailabilities = _context.Devices
					.Select(d => new DeviceAvailabilityDto
					{
						DeviceName = d.DeviceName,
						Status = d.DeviceStatus
					}).ToList(),

				MaintenanceSummary = new MaintenanceSummaryDto
				{
					SuccessRate = CalculateMaintenanceSuccessRate(filteredRequests),
					AverageRepairTimeDays = CalculateAverageRepairTime(filteredRequests),
					CompletedRequests = maintenanceRequests.Count(r => r.AdminStatus == "Completed")
				}
			};

			return View(model);
		}



		// ============================
		// دالة لحساب نسبة نجاح الصيانة
		// Calculates maintenance success rate
		// ============================
		private int CalculateMaintenanceSuccessRate(List<Request> filteredRequests)
		{
			int totalRequests = filteredRequests.Count(r => r.RequestType == "Maintenance");
			int completedRequests = filteredRequests.Count(r => r.RequestType == "Maintenance" && r.AdminStatus == "Completed");
			return totalRequests == 0 ? 0 : (int)Math.Round((double)completedRequests / totalRequests * 100);
		}


		// ============================
		// دالة لحساب متوسط مدة الصيانة بالأيام
		// Calculates average repair duration in days
		// ============================
		private double CalculateAverageRepairTime(List<Request> filteredRequests)
		{
			var completedMaintenanceRequests = filteredRequests
				.Where(r => r.RequestType == "Maintenance" && r.AdminStatus == "Completed" && r.RequestDate != null && r.DeviceId != null)
				.Join(
					_context.Devices,
					r => r.DeviceId,
					d => d.DeviceID,
					(r, d) => new { r.RequestDate, d.LastMaintenance }
				)
				.Where(x => x.LastMaintenance != null)
                .Select(x => (x.LastMaintenance.Value - x.RequestDate).Days)
                .ToList();

			if (completedMaintenanceRequests.Count == 0)
				return 0;

			return Math.Round(completedMaintenanceRequests.Average(), 2);
		}

      


    }
}
