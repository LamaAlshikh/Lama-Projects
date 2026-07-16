// ===========================================================
// الكنترولر: TechnologiesController
// الوظيفة: إدارة الفئات التقنية (إضافة، تعديل، حذف)
// Controller: TechnologiesController
// Purpose: Manage technology categories (Add, Edit, Delete)
// ===========================================================

using WepApp2.Data;
using WepApp2.Models;
using Microsoft.AspNetCore.Mvc;

namespace WepApp2.Controllers
{
    public class TechnologiesController : Controller
    {
        private readonly AppDbContext _context;

        public TechnologiesController(AppDbContext context)
        {
            _context = context;
        }

		// ============================
		// عرض الصفحة الرئيسية للفئات التقنية
		// Display technology management page (form + table)
		// ============================
		public IActionResult Index()
        {
            var viewModel = new TechnologyPageViewModel
            {
                Technologies = _context.Technologies.ToList(),
                Technology = new Technology(),
                IsEdit = false
            };

            return View("technology", viewModel);
        }



		// ============================
		// إضافة فئة تقنية جديدة (POST)
		// Handle POST request to create new technology
		// ============================
		[HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Technology technology)
        {
            if (!ModelState.IsValid)
            {
                var viewModel = new TechnologyPageViewModel
                {
                    Technologies = _context.Technologies.ToList(),
                    Technology = technology,
                    IsEdit = false
                };
                return View("technology", viewModel);
            }

            var exists = _context.Technologies
                .Any(t => t.TechnologyName.Trim().ToLower() == technology.TechnologyName.Trim().ToLower());

            if (exists)
            {
                ModelState.AddModelError("TechnologyName", "⚠️ هذا النوع موجود بالفعل.");

                var viewModel = new TechnologyPageViewModel
                {
                    Technologies = _context.Technologies.ToList(),
                    Technology = technology,
                    IsEdit = false
                };
                return View("technology", viewModel);
            }

            _context.Technologies.Add(technology);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }



		// ============================
		// تعبئة النموذج للتعديل
		// Populate form for editing a technology
		// ============================
		public IActionResult Edit(int id)
        {
            var tech = _context.Technologies
    .Where(t => t.TechnologyID == id)
    .Select(t => new Technology
    {
        TechnologyID = t.TechnologyID,
        TechnologyName = t.TechnologyName,
        TechnologyDescription = t.TechnologyDescription
    }).FirstOrDefault();

            if (tech == null)
                return NotFound();

            var viewModel = new TechnologyPageViewModel
            {
                Technologies = _context.Technologies.ToList(),
                Technology = tech,
                IsEdit = true
            };

            return View("technology", viewModel);
        }


		// ============================
		// حفظ التعديلات على الفئة التقنية
		// Save updated technology info
		// ============================
		[HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update([Bind(Prefix = "Technology")] Technology updatedTech)

        {
            Console.WriteLine("🚨 REACHED UPDATE");
            Console.WriteLine("ID = " + updatedTech.TechnologyID);
            Console.WriteLine("Name = " + updatedTech.TechnologyName);

            if (!ModelState.IsValid)
            {
                var viewModel = new TechnologyPageViewModel
                {
                    Technologies = _context.Technologies.ToList(),
                    Technology = updatedTech,
                    IsEdit = true
                };
                return View("technology", viewModel);
            }

            var tech = _context.Technologies.Find(updatedTech.TechnologyID);
            if (tech == null)
                return NotFound();

            tech.TechnologyName = updatedTech.TechnologyName;
            tech.TechnologyDescription = updatedTech.TechnologyDescription;

            _context.SaveChanges();
            
            return RedirectToAction("Index");
        }


		// ============================
		// حذف فئة تقنية
		// Delete a technology by ID
		// ============================
		public IActionResult Delete(int id)
        {
            var tech = _context.Technologies.Find(id);
            if (tech == null)
                return NotFound();

            _context.Technologies.Remove(tech);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
