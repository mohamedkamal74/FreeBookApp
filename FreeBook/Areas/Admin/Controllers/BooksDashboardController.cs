using Domin.Entity;
using Infrastructure.Common;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeBook.Areas.Admin.Controllers
{
    [Area(Helper.Admin)]
    [Authorize(Roles = "Admin")]
    public class BooksDashboardController : Controller
    {
        private readonly IDashboardRepository _dashboardRepository;

        public BooksDashboardController(IDashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository;
        }

        public async Task<IActionResult> Index(DateTime? from = null, DateTime? to = null, Guid? authorId = null, Guid? categoryId = null)
        {
            var result = await _dashboardRepository.GetBooksAsync(new BooksDashboardQuery
            {
                From = from,
                To = to,
                AuthorId = authorId,
                CategoryId = categoryId
            });

            return View(result.ValueOr(new BooksDashboardData()));
        }
    }
}
