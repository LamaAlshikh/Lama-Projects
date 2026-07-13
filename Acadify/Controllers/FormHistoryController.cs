using Acadify.Models;
using Acadify.Models.Db;
using Acadify.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Db = Acadify.Models.Db;

namespace Acadify.Controllers
{
    public class FormHistoryController : Controller
    {
        private readonly Db.AcadifyDbContext _context;

        public FormHistoryController(Db.AcadifyDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> FormHistory(int studentId, string formType)
        {
            if (studentId <= 0 || string.IsNullOrWhiteSpace(formType))
                return BadRequest();

            var requestedType = NormalizeFormType(formType);

            var allForms = await _context.Forms
                .Where(f => f.StudentId == studentId)
                .OrderByDescending(f => f.FormDate)
                .ThenByDescending(f => f.FormId)
                .ToListAsync();

            var matchedForms = allForms
                .Where(f => NormalizeFormType(f.FormType) == requestedType)
                .ToList();

            

            var vm = new FormHistoryVM
            {
                StudentId = studentId,
                FormType = formType,
                PageTitle = $"{formType} History",
                Forms = matchedForms
                    .OrderByDescending(f => f.FormDate)
                    .ThenByDescending(f => f.FormId)
                    .Select(f => new FormHistoryItemVM
                    {
                        FormId = f.FormId,
                        FormType = f.FormType,
                        FormStatus = f.FormStatus,
                        FormDate = f.FormDate
                    })
                    .ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ViewForm(int id, string formType)
        {
            var requestedType = NormalizeFormType(formType);

            var form = await _context.Forms
                .Include(f => f.Student)
                .FirstOrDefaultAsync(f => f.FormId == id);

            if (form == null)
                return NotFound();

            if (requestedType == "FORM1")
            {
                return RedirectToAction("Form1", "Advisor", new { studentId = form.StudentId });
            }

            if (requestedType == "FORM2")
            {
                var item = await _context.Forms
                    .Include(f => f.Student)
                    .Include(f => f.CourseChoiceMonitoringForm)
                    .FirstOrDefaultAsync(f => f.FormId == id);

                if (item == null || item.CourseChoiceMonitoringForm == null)
                    return NotFound();

                var form2 = item.CourseChoiceMonitoringForm;

                var selectedCourses = string.IsNullOrWhiteSpace(form2.SelectedCoursesJson)
                    ? new List<SelectedCourseVM>()
                    : JsonSerializer.Deserialize<List<SelectedCourseVM>>(form2.SelectedCoursesJson)
                      ?? new List<SelectedCourseVM>();

                var vm = new Form2ViewModel
                {
                    StudentName = item.Student?.Name ?? "",
                    StudentId = item.StudentId,
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

                return View("~/Views/Advisor/Form2.cshtml", vm);
            }

            if (requestedType == "FORM3")
            {
                return RedirectToAction("ViewSavedForm3", "Advisor", new { formId = id });
            }

            if (requestedType == "FORM4")
            {
                return RedirectToAction("ViewSavedForm4", "Advisor", new { formId = id });
            }

            if (requestedType == "FORM5")
            {
                var item = await _context.GraduationProjectEligibilityForms
                    .Include(x => x.Form)
                    .ThenInclude(f => f.Student)
                    .FirstOrDefaultAsync(x => x.FormId == id);

                if (item == null || item.Form == null)
                    return NotFound();

                var vm = BuildHistoryForm5Vm(item);

                return View("~/Views/GraduationProjectEligibility/Form5.cshtml", vm);
            }

            return NotFound();
        }

        private static string NormalizeFormType(string? value)
        {
            var v = (value ?? string.Empty)
                .Trim()
                .ToUpper()
                .Replace(" ", "");

            if (v == "FORM1" || v == "ACADEMICADVISINGFORM")
                return "FORM1";

            if (v == "FORM2" || v == "COURSESELECTIONFORM" || v == "COURSECHOICEMONITORINGFORM")
                return "FORM2";

            if (v == "FORM3" || v == "MEETINGFORM")
                return "FORM3";

            if (v == "FORM4" || v == "STUDYPLANMATCHINGFORM")
                return "FORM4";

            if (v == "FORM5" || v == "GRADUATIONPROJECTELIGIBILITYFORM")
                return "FORM5";

            return v;
        }

        private static GraduationProjectEligibilityFormVM BuildHistoryForm5Vm(Db.GraduationProjectEligibilityForm entity)
        {
            var vm = new GraduationProjectEligibilityFormVM
            {
                FormId = entity.FormId,
                Form = entity.Form!,
                StudentName = entity.Form?.Student?.Name ?? "-",
                StudentId = entity.Form?.StudentId.ToString() ?? "-",
                Eligibility = entity.Eligibility,
                RequiredCoursesStatus = entity.RequiredCoursesStatus,
                IsHistoryView = true,
                IsEditMode = false
            };

            var map = ParseSnapshot(entity.RequiredCoursesStatus);

            vm.CPIS351 = GetBool(map, "CPIS351");
            vm.CPIS358 = GetBool(map, "CPIS358");
            vm.CPIS323 = GetBool(map, "CPIS323");
            vm.CPIS380 = GetBool(map, "CPIS380");
            vm.CPIS357 = GetBool(map, "CPIS357");
            vm.CPIS342 = GetBool(map, "CPIS342");

            vm.IsEligible = string.Equals(entity.Eligibility, "Eligible", StringComparison.OrdinalIgnoreCase);

            return vm;
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

                var key = part.Substring(0, index).Trim();
                var value = part[(index + 1)..].Trim();

                if (!map.ContainsKey(key))
                    map.Add(key, value);
            }

            return map;
        }

        private static bool GetBool(Dictionary<string, string> map, string key)
        {
            if (!map.TryGetValue(key, out var value))
                return false;

            return value == "1" ||
                   value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }
    }
}