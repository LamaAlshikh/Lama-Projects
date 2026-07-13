using System;
using Acadify.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Db = Acadify.Models.Db;
using UserModel = Acadify.Models.Db.User;
using AdminModel = Acadify.Models.Db.Admin;
using AdvisorModel = Acadify.Models.Db.Advisor;
using StudentModel = Acadify.Models.Db.Student;

namespace Acadify.Controllers
{
    public class AccountController : Controller
    {
        private readonly Db.AcadifyDbContext _db;

        public AccountController(Db.AcadifyDbContext db)
        {
            _db = db;
        }

        // ==============================
        // Sign Up
        // ==============================

        [HttpGet]
        public IActionResult SignUp()
        {
            return View(new SignUpVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignUp(SignUpVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string email = model.Email
                .Trim()
                .ToLower();

            bool isStudentEmail =
                email.EndsWith(
                    "@stu.kau.edu.sa",
                    StringComparison.OrdinalIgnoreCase
                );

            bool isStaffEmail =
                email.EndsWith(
                    "@kau.edu.sa",
                    StringComparison.OrdinalIgnoreCase
                )
                && !isStudentEmail;

            // Only KAU university emails are accepted.
            if (!isStudentEmail && !isStaffEmail)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Only KAU university email addresses are allowed."
                );

                return View(model);
            }

            bool emailExists = await _db
                .Set<UserModel>()
                .AnyAsync(u => u.Email.ToLower() == email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "This email is already registered."
                );

                return View(model);
            }

            int? newStudentId = null;

            // Student ID is required only for students.
            if (isStudentEmail)
            {
                if (!int.TryParse(model.ID, out int studentId))
                {
                    ModelState.AddModelError(
                        nameof(model.ID),
                        "The student ID must contain numbers only."
                    );

                    return View(model);
                }

                bool studentExists = await _db
                    .Set<StudentModel>()
                    .AnyAsync(s => s.StudentId == studentId);

                if (studentExists)
                {
                    ModelState.AddModelError(
                        nameof(model.ID),
                        "This student ID is already registered."
                    );

                    return View(model);
                }

                newStudentId = studentId;
            }

            try
            {
                // Create the main user account.
                var user = new UserModel
                {
                    Name = model.FullName.Trim(),
                    Email = email,
                    Password = model.Password
                };

                _db.Set<UserModel>().Add(user);
                await _db.SaveChangesAsync();

                // Create the student record.
                if (isStudentEmail)
                {
                    var student = new StudentModel
                    {
                        StudentId = newStudentId!.Value,
                        UserId = user.UserId,
                        Name = model.FullName.Trim(),
                        Major = "Information Systems",
                        CompletedHours = 0
                    };

                    _db.Set<StudentModel>().Add(student);
                    await _db.SaveChangesAsync();
                }

                // Create the advisor record for every staff account.
                if (isStaffEmail)
                {
                    await EnsureAdvisorForUserAsync(user);
                }

                TempData["SuccessMessage"] =
                    "Your account was created successfully. You can now log in.";

                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "An error occurred while creating the account: "
                    + ex.Message
                );

                return View(model);
            }
        }

        // ==============================
        // Login
        // ==============================

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string email = model.Email
                .Trim()
                .ToLower();

            var user = await _db
                .Set<UserModel>()
                .FirstOrDefaultAsync(u =>
                    u.Email.ToLower() == email
                    && u.Password == model.Password
                );

            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The email or password is incorrect."
                );

                return View(model);
            }

            bool isStudentEmail =
                email.EndsWith(
                    "@stu.kau.edu.sa",
                    StringComparison.OrdinalIgnoreCase
                );

            bool isStaffEmail =
                email.EndsWith(
                    "@kau.edu.sa",
                    StringComparison.OrdinalIgnoreCase
                )
                && !isStudentEmail;

            // ==============================
            // Student Login
            // ==============================

            if (isStudentEmail)
            {
                var student =
                    await FindStudentForUserAsync(user);

                if (student == null)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Student record was not found."
                    );

                    return View(model);
                }

                // Save student information in the session.
                HttpContext.Session.SetInt32(
                    "UserId",
                    user.UserId
                );

                HttpContext.Session.SetString(
                    "UserName",
                    user.Name
                );

                HttpContext.Session.SetString(
                    "StudentName",
                    user.Name
                );

                HttpContext.Session.SetString(
                    "UserEmail",
                    user.Email
                );

                HttpContext.Session.SetString(
                    "UserRole",
                    "Student"
                );

                HttpContext.Session.SetInt32(
                    "StudentId",
                    student.StudentId
                );

                // The student selects an advisor first.
                if (!student.AdvisorId.HasValue
                    || student.AdvisorId.Value <= 0)
                {
                    return RedirectToAction(
                        "SelectAdvisor",
                        "Student"
                    );
                }

                // Check whether the student uploaded a transcript.
                bool hasTranscript = await _db
                    .Set<Db.Transcript>()
                    .AnyAsync(t =>
                        t.StudentId == student.StudentId
                        && !string.IsNullOrWhiteSpace(t.PdfFile)
                    );

                if (!hasTranscript)
                {
                    return RedirectToAction(
                        "UploadTranscript",
                        "Student"
                    );
                }

                return RedirectToAction(
                    "StudentHome",
                    "Student"
                );
            }

            // ==============================
            // Staff Login
            // ==============================

            if (!isStaffEmail)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Only KAU university email addresses are allowed."
                );

                return View(model);
            }

            string selectedRole =
                model.Role?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(selectedRole))
            {
                ModelState.AddModelError(
                    nameof(model.Role),
                    "Please choose Advisor or Admin."
                );

                return View(model);
            }

            // Save the common staff information.
            HttpContext.Session.SetInt32(
                "UserId",
                user.UserId
            );

            HttpContext.Session.SetString(
                "UserName",
                user.Name
            );

            HttpContext.Session.SetString(
                "UserEmail",
                user.Email
            );

            // ==============================
            // Login as Advisor
            // ==============================

            if (selectedRole.Equals(
                    "Advisor",
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                int advisorId =
                    await EnsureAdvisorForUserAsync(user);

                HttpContext.Session.SetString(
                    "UserRole",
                    "Advisor"
                );

                HttpContext.Session.SetInt32(
                    "AdvisorId",
                    advisorId
                );

                // Remove old Admin session information.
                HttpContext.Session.Remove("AdminId");

                return RedirectToAction(
                    "AdvisorHome",
                    "Advisor"
                );
            }

            // ==============================
            // Login as Admin
            // ==============================

            if (selectedRole.Equals(
                    "Admin",
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                // Create an Admin record during the first Admin login.
                int adminId =
                    await EnsureAdminForUserAsync(user);

                HttpContext.Session.SetString(
                    "UserRole",
                    "Admin"
                );

                HttpContext.Session.SetInt32(
                    "AdminId",
                    adminId
                );

                // Remove old Advisor session information.
                HttpContext.Session.Remove("AdvisorId");

                return RedirectToAction(
                    "ManageAdvisorRequests",
                    "Admin"
                );
            }

            ModelState.AddModelError(
                nameof(model.Role),
                "Please choose a valid role."
            );

            return View(model);
        }

        // ==============================
        // Helper Methods
        // ==============================

        private async Task<StudentModel?>
            FindStudentForUserAsync(UserModel user)
        {
            return await _db
                .Set<StudentModel>()
                .FirstOrDefaultAsync(
                    s => s.UserId == user.UserId
                );
        }

        private async Task<int>
            EnsureAdminForUserAsync(UserModel user)
        {
            var admin = await _db
                .Set<AdminModel>()
                .FirstOrDefaultAsync(
                    a => a.UserId == user.UserId
                );

            if (admin != null)
            {
                return admin.AdminId;
            }

            var newAdmin = new AdminModel
            {
                UserId = user.UserId
            };

            _db.Set<AdminModel>().Add(newAdmin);
            await _db.SaveChangesAsync();

            return newAdmin.AdminId;
        }

        private async Task<int>
            EnsureAdvisorForUserAsync(UserModel user)
        {
            var advisor = await _db
                .Set<AdvisorModel>()
                .FirstOrDefaultAsync(
                    a => a.UserId == user.UserId
                );

            if (advisor != null)
            {
                return advisor.AdvisorId;
            }

            int nextAdvisorId;

            bool advisorRecordsExist = await _db
                .Set<AdvisorModel>()
                .AnyAsync();

            if (advisorRecordsExist)
            {
                nextAdvisorId = await _db
                    .Set<AdvisorModel>()
                    .MaxAsync(a => a.AdvisorId) + 1;
            }
            else
            {
                nextAdvisorId = 1;
            }

            var newAdvisor = new AdvisorModel
            {
                AdvisorId = nextAdvisorId,
                UserId = user.UserId,
                Department = "Information Systems"
            };

            _db.Set<AdvisorModel>().Add(newAdvisor);
            await _db.SaveChangesAsync();

            return newAdvisor.AdvisorId;
        }

        // ==============================
        // Logout
        // ==============================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction(nameof(Login));
        }
    }
}