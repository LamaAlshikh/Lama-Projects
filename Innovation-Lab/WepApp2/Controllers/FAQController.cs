using Microsoft.AspNetCore.Mvc;

namespace FAQ.Controllers
{
    public class FAQController : Controller
    {
        public IActionResult FAQ()
        {
            return View();
        }
    }
}
