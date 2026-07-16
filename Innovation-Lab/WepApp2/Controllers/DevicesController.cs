// ===========================================================
// الكنترولر: DevicesController
// الوظيفة: إدارة جميع عمليات CRUD المرتبطة بالأجهزة
// Controller: DevicesController
// Purpose: Manages all CRUD operations related to devices
// ===========================================================

using WepApp2.Data;
using WepApp2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WepApp2.Controllers
{
    public class DevicesController : Controller
    {
        private readonly AppDbContext _context;

        public DevicesController(AppDbContext context)
        {
            _context = context;
        }


		// ============================
		// عرض قائمة الأجهزة + ملخص الصيانة
		// Display device list + maintenance summary
		// ============================
		public IActionResult Devices()
        {
            var devices = _context.Devices
                .Include(d => d.Technology) 
                .ToList();

            var MaintenanceSummary = new MaintenanceSummaryDto
            {
                SuccessRate = CalculateMaintenanceSuccessRate(),
                AverageRepairTimeDays = CalculateAverageRepairTime(),
                CompletedRequests = _context.Requests.Count(r => r.RequestType == "Maintenance" && r.AdminStatus == "Completed")
            };

            ViewBag.MaintenanceSummary = MaintenanceSummary; 

            return View(devices);
        }


		// ============================
		// نسبة النجاح في طلبات الصيانة
		// Maintenance success rate
		// ============================
		private int CalculateMaintenanceSuccessRate()
        {
            int totalRequests = _context.Requests.Count(r => r.RequestType == "Maintenance");
            int completedRequests = _context.Requests.Count(r => r.RequestType == "Maintenance" && r.AdminStatus == "Completed");
            return totalRequests == 0 ? 0 : (int)Math.Round((double)completedRequests / totalRequests * 100);
        }



        // ============================
        // متوسط مدة الإصلاح بالأيام
        // Average repair time in days
        // ============================
        private double CalculateAverageRepairTime()
        {
            var completedMaintenanceRequests = _context.Requests
                .Where(r => r.RequestType == "Maintenance" && r.AdminStatus == "Completed" && r.RequestDate != null && r.DeviceId != null)
                .Join(
                    _context.Devices,
                    r => r.DeviceId,
                    d => d.DeviceID,
                    (r, d) => new { r.RequestDate, d.LastMaintenance }
                )
                .Where(x => x.LastMaintenance != null)
                .Select(x => (x.LastMaintenance.Value - x.RequestDate).TotalDays)
                .ToList();

            if (completedMaintenanceRequests.Count == 0)
                return 0;

            return Math.Round(completedMaintenanceRequests.Average(), 2);
        }



        // ============================
        // عرض نموذج إضافة جهاز جديد
        // Display form to add new device
        // ============================
        public IActionResult AddDevice()
        {
            ViewBag.Technologies = _context.Technologies.ToList(); // ✅ بدون .Select
            return View("add-device", new Device());
        }


		// ============================
		// إضافة جهاز جديد (POST)
		// Handle POST to add device
		// ============================
		[HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddDevice(Device device)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Technologies = _context.Technologies.ToList(); // ✅ بدون .Select
                return View("add-device", device);
            }

            device.LastUpdate = DateTime.Now;
            _context.Devices.Add(device);
            _context.SaveChanges();

            return RedirectToAction("Devices");
        }


		// ============================
		// عرض نموذج تعديل جهاز
		// Display form to edit existing device
		// ============================
		[HttpGet]
        public IActionResult EditDevice(int id)
        {
            var device = _context.Devices.Find(id);
            if (device == null)
                return NotFound();

            ViewBag.Technologies = _context.Technologies
                .Select(t => new { t.TechnologyID, t.TechnologyName })
                .ToList();

            return View("edit-device", device);
        }


		// ============================
		// تعديل جهاز (POST)
		// Handle POST to update device
		// ============================
		[HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditDevice(Device updatedDevice)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Technologies = _context.Technologies
                    .ToList();

                return View("edit-device", updatedDevice);
            }

            var device = _context.Devices.Find(updatedDevice.DeviceID);
            if (device == null)
                return NotFound();

			// تحديث بيانات الجهاز
			device.DeviceName = updatedDevice.DeviceName;
            device.TechnologyId = updatedDevice.TechnologyId;
            device.SerialNumber = updatedDevice.SerialNumber;
            device.DeviceLocation = updatedDevice.DeviceLocation;
            device.BrandName = updatedDevice.BrandName;
            device.DeviceModel = updatedDevice.DeviceModel;
            device.DeviceStatus = updatedDevice.DeviceStatus ?? "inactive";
            device.LastUpdate = DateTime.Now;

            _context.SaveChanges();

            return RedirectToAction("Devices");
        }


		// ============================
		// تغيير حالة الجهاز (نشط، صيانة، غير متصل...)
		// Update device status (active, maintenance, offline...)
		// ============================
		[HttpGet]
        public IActionResult SetStatus(int id, string status)
        {
            var device = _context.Devices.Find(id);
            if (device == null)
                return NotFound();

            device.DeviceStatus = status;

            if (status == "maintenance")
                device.LastMaintenance = DateTime.Now;

            device.LastUpdate = DateTime.Now;

			if (status.ToLower() == "inactive")
			{
				device.IsDeleted = true;
			}
			else
			{
				device.IsDeleted = false; 
			}

			_context.SaveChanges();
			return RedirectToAction("Devices");
        }




    }
}
