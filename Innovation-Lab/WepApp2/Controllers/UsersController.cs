using WepApp2.Data;
using WepApp2.Models;
using Microsoft.AspNetCore.Mvc;

// ✅ وحدة تحكم إدارة المستخدمين
// ✅ Users Controller
public class UsersController : Controller
{
    private readonly AppDbContext _context;

    // ✅ التهيئة باستخدام قاعدة البيانات
    // ✅ Inject database context via constructor
    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    // ✅ عرض جميع المستخدمين
    // ✅ Display all users
    public IActionResult Users()
    {
        var users = _context.Users.ToList();
        return View(users);
    }

    // ✅ عرض نموذج إضافة مستخدم جديد
    // ✅ Show Add User form
    public IActionResult AddUser()
    {
        var user = new User(); // إنشاء كائن جديد لتفادي null

        return View(user);     // تمريره للـ View
    }

    // ✅ تنفيذ إضافة المستخدم عند الضغط على زر "إرسال"
    // ✅ Handle Add User form submission
    [HttpPost]
    public IActionResult AddUser(User user)
    {

        // ✅ التحقق من صحة النموذج
        // ✅ Validate the model
        if (!ModelState.IsValid)
        {

            return View(user);
        }

        // ✅ إذا اختار المستخدم "أخرى"، يتم استخدام النص المدخل
        // ✅ Handle "Other" faculty case
        if (user.Faculty == "أخرى")
        {
            user.Faculty = user.OtherFaculty;
        }

        // ✅ تعيين وقت تسجيل الدخول الأخير للحظة الإضافة
        // ✅ Set initial LastLogin date
        user.LastLogIn = DateTime.Now;

        // 🟢 الحل البديل: توليد ID يدويًا (غير مستخدم هنا)
        //int maxId = _context.Users.Any() ? _context.Users.Max(u => u.UserID) : 0;
        //user.UserID = maxId + 1;

        // ✅ حفظ المستخدم في قاعدة البيانات
        // ✅ Save user to the database
        _context.Users.Add(user);
        _context.SaveChanges();

        TempData["Message"] = "تمت الإضافة بنجاح!";
        return RedirectToAction("Users"); // إعادة التوجيه لقائمة المستخدمين
    }

    // ✅ عرض نموذج تعديل بيانات المستخدم
    // ✅ Show Edit User form
    public IActionResult EditUser(int id)
    {
        var user = _context.Users.Find(id);

        // ✅ إعداد قيمة OtherFaculty حسب نوع الجهة
        // ✅ Setup OtherFaculty based on current Faculty value
        if (user.Faculty == "أخرى")
        {
            user.OtherFaculty = ""; // يملأها المستخدم لاحقًا
        }
        else
        {
            user.OtherFaculty = user.Faculty; // عرضها مباشرة
        }


        if (user == null)
        {
            return NotFound();
        }

        // ✅ تمرير قائمة الكليات إلى الـ View
        // ✅ Send faculties list to the view
        ViewBag.Faculties = new List<string>
    {
          "العمادات",
    "الكلية التطبيقية",
    "الدراسات العليا",
    "كلية الآداب والعلوم الإنسانية",
    "كلية الاقتصاد والإدارة",
    "كلية الاتصال والإعلام",
    "كلية التربية",
    "كلية التمريض",
    "كلية الحاسبات وتقنية المعلومات",
    "كلية الحقوق",
    "كلية الطب",
    "كلية الطب الأسنان",
    "كلية الصيدلة",
    "كلية العلوم",
    "كلية العلوم البيئية",
    "كلية العلوم الطبية التطبيقية",
    "كلية السياحة",
    "كلية العمارة والتخطيط",
    "كلية الهندسة",
    "كلية الدراسات البحرية",
    "كلية علوم الإنسان والتصاميم",
    "كلية علوم الأرض",
    "كلية علوم البحار",
    "كلية علوم التأهيل الطبي",
    "أخرى"
    };

        return View(user);
    }

    // ✅ تنفيذ تعديل بيانات المستخدم
    // ✅ Handle Edit User form submission
    [HttpPost]
    public IActionResult EditUser(User updatedUser)
    {

        // ✅ التعامل مع حالة "أخرى" للجهة
        // ✅ Handle "Other" faculty input
        if (updatedUser.Faculty == "أخرى")
        {
            updatedUser.Faculty = updatedUser.OtherFaculty;
        }

        // ✅ إعادة عرض الصفحة في حال وجود أخطاء
        // ✅ Redisplay form if validation fails
        if (!ModelState.IsValid)
        {
            // إعادة عرض الفورم مع الأخطاء إذا وجد
            return View(updatedUser);
        }

        // ✅ إيجاد المستخدم من قاعدة البيانات
        // ✅ Retrieve existing user from database
        var existingUser = _context.Users.Find(updatedUser.UserID);
        if (existingUser == null)
        {
            return NotFound();
        }



        // ✅ تحديث جميع الخصائص القابلة للتعديل
        // ✅ Update editable fields
        existingUser.FirstName = updatedUser.FirstName;
        existingUser.LastName = updatedUser.LastName;
        existingUser.Email = updatedUser.Email;
        existingUser.UserName = updatedUser.UserName;
        existingUser.PhoneNumber = updatedUser.PhoneNumber;
        existingUser.UserRole = updatedUser.UserRole;
        existingUser.Faculty = updatedUser.Faculty;
        existingUser.Department = updatedUser.Department;
        existingUser.UserPassWord = updatedUser.UserPassWord;
        existingUser.IsActive = updatedUser.IsActive;
        // 👉 يمكنك تحديث خصائص إضافية هنا إن وجدت

        _context.SaveChanges();

        TempData["Message"] = "تم تحديث المستخدم بنجاح!";
        return RedirectToAction("Users");
    }

   

}