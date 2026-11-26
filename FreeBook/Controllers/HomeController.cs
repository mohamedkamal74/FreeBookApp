using System.Diagnostics;
using FreeBook.Models;
using Infrastructure.Common;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace FreeBook.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IPublicHomeQueryService _publicHomeQueries;

        public HomeController(ILogger<HomeController> logger, IPublicHomeQueryService publicHomeQueries)
        {
            _logger = logger;
            _publicHomeQueries = publicHomeQueries;
        }

        public async Task<IActionResult> Index()
        {
            var result = await _publicHomeQueries.GetAvailableBooksAsync();
            var model = new PublicHomeViewModel
            {
                AvailableBooks = result.ValueOr(new List<AvailableBookCardDto>())
            };
            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
