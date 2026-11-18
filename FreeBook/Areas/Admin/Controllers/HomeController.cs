using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeBook.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        [AllowAnonymous]
        public IActionResult Denied()
        {
            return View();
        }
    }
}
