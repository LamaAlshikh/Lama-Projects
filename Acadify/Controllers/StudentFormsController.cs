using Microsoft.AspNetCore.Mvc;

namespace Acadify.Controllers
{
    public class StudentFormsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
