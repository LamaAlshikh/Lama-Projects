using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Claims;
using WepApp2.Data;
using WepApp2.Models;
using WepApp2.EmailService;
using WepApp2.EmailService;

namespace WepApp2.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IDataProtector _protector;

        public AuthController(AppDbContext context, IEmailService emailService, IDataProtectionProvider dataProtectionProvider)
        {
            _context = context;
            _emailService = emailService;
            _protector = dataProtectionProvider.CreateProtector("PasswordReset");
        }

        // GET: Login - دالة واحدة فقط
        [HttpGet]
        public IActionResult Login()
        {
            // تمرير TempData إلى ViewBag
            if (TempData["ResetSuccess"] != null)
            {
                ViewBag.ResetSuccess = true;
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(User user)
        {
            var existingUser = _context.Users
            .FirstOrDefault(u => u.UserName == user.UserName && u.UserPassWord == user.UserPassWord);

            if (existingUser != null)
            {
                // إنشاء قائمة الـ Claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, existingUser.UserName),
                    new Claim(ClaimTypes.Role, existingUser.UserRole ?? "Student"),
                    new Claim("FirstName", existingUser.FirstName ?? "")  // ✅ هنا نضيف الاسم الأول
                    ,
            new Claim("UserID", existingUser.UserID.ToString()) // <-- هنا أضفنا الـ UserId
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                // تسجيل الدخول بالكوكيز
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                // التوجيه حسب الدور
                if (existingUser.UserRole == "مدير")
                    return RedirectToAction("Index", "Dashboard");

                if (existingUser.UserRole == "مشرف")
                    return RedirectToAction("Index", "Supervisor");

                return RedirectToAction("HomePage", "Auth");
            }

            ViewBag.LoginFailed = true;
            return View(user);
        }

        // GET: Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(User user, string ConfirmPassword)
        {
            if (user.UserPassWord != ConfirmPassword)
            {
                ViewBag.PasswordMismatch = true;
                return View(user);
            }

            if (string.IsNullOrWhiteSpace(user.UserPassWord))
            {
                ViewBag.PasswordEmpty = true;
                return View(user);
            }

            var existingUser = _context.Users
                .FirstOrDefault(u => u.UserName == user.UserName || u.Email == user.Email);

            if (existingUser != null)
            {
                ViewBag.UserExists = true;
                return View(user);
            }

            if (ModelState.IsValid)
            {
                user.LastLogIn = DateTime.Now;
                user.IsActive = true;

                _context.Users.Add(user);
                _context.SaveChanges();

                return RedirectToAction("Login");
            }

            return View(user);
        }

        // Dashboard view
        [HttpGet]
        public IActionResult HomePage()
        {
            return View();
        }

        // GET: Forgot Password
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Forgot Password
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = true;
                ViewBag.ErrorMessage = "يرجى إدخال البريد الإلكتروني";
                return View();
            }

            if (!email.EndsWith("@kau.edu.sa") && !email.EndsWith("@stu.kau.edu.sa"))
            {
                ViewBag.Error = true;
                ViewBag.ErrorMessage = "يجب استخدام البريد الإلكتروني الجامعي";
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (user != null)
            {
                // إنشاء token مشفر
                var tokenData = $"{user.UserID}|{DateTime.UtcNow.AddHours(1):yyyy-MM-dd HH:mm:ss}";
                var encryptedToken = _protector.Protect(tokenData);

                // تحويل Token إلى Base64 URL-safe
                var urlSafeToken = Uri.EscapeDataString(encryptedToken);

                // إنشاء رابط الاستعادة
                var resetLink = $"{Request.Scheme}://{Request.Host}/Auth/ResetPassword?token={urlSafeToken}";

                try
                {
                    await _emailService.SendPasswordResetEmail(user.Email, user.FirstName, resetLink);

                    TempData["EmailSent"] = "true";
                    return RedirectToAction("ForgotPasswordConfirmation");
                }
                catch (Exception)
                {
                    ViewBag.Error = true;
                    ViewBag.ErrorMessage = "حدث خطأ في إرسال البريد الإلكتروني. يرجى المحاولة لاحقاً.";
                    return View();
                }
            }
            else
            {
                // لا نخبر المستخدم أن البريد غير موجود (لأسباب أمنية)
                TempData["EmailSent"] = "true";
                return RedirectToAction("ForgotPasswordConfirmation");
            }
        }

        // GET: Forgot Password Confirmation
        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            if (TempData["EmailSent"] == null)
            {
                return RedirectToAction("Login");
            }
            return View();
        }

        // GET: Reset Password
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login");
            }

            try
            {
                // فك تشفير Token
                var decodedToken = Uri.UnescapeDataString(token);
                var decryptedData = _protector.Unprotect(decodedToken);

                // استخراج البيانات
                var parts = decryptedData.Split('|');
                if (parts.Length != 2)
                {
                    TempData["Error"] = "رابط غير صالح";
                    return RedirectToAction("Login");
                }

                var userId = int.Parse(parts[0]);
                var expiry = DateTime.Parse(parts[1]);

                // التحقق من صلاحية الرابط
                if (DateTime.UtcNow > expiry)
                {
                    TempData["Error"] = "انتهت صلاحية الرابط";
                    return RedirectToAction("ForgotPassword");
                }

                // الحصول على المستخدم
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    TempData["Error"] = "مستخدم غير موجود";
                    return RedirectToAction("Login");
                }

                ViewBag.Token = token;
                ViewBag.Email = user.Email;
                return View();
            }
            catch
            {
                TempData["Error"] = "رابط غير صالح";
                return RedirectToAction("Login");
            }
        }

        // POST: Reset Password
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string token, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login");
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = true;
                ViewBag.ErrorMessage = "كلمات المرور غير متطابقة";
                ViewBag.Token = token;
                return View();
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                ViewBag.Error = true;
                ViewBag.ErrorMessage = "يرجى إدخال كلمة مرور صحيحة";
                ViewBag.Token = token;
                return View();
            }

            try
            {
                // فك تشفير Token
                var decodedToken = Uri.UnescapeDataString(token);
                var decryptedData = _protector.Unprotect(decodedToken);

                var parts = decryptedData.Split('|');
                var userId = int.Parse(parts[0]);
                var expiry = DateTime.Parse(parts[1]);

                // التحقق من صلاحية الرابط
                if (DateTime.UtcNow > expiry)
                {
                    TempData["Error"] = "انتهت صلاحية الرابط";
                    return RedirectToAction("ForgotPassword");
                }

                // تحديث كلمة المرور
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.UserPassWord = newPassword;
                    await _context.SaveChangesAsync();

                    return RedirectToAction("Login");
                }
            }
            catch
            {
                TempData["Error"] = "حدث خطأ في تحديث كلمة المرور";
            }

            return RedirectToAction("Login");
        }


        // تسجيل الخروج
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}