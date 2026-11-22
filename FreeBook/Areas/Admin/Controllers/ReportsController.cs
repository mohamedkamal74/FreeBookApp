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
    public class ReportsController : Controller
    {
        private readonly IReportRepository _reportRepository;

        public ReportsController(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> OverdueBooks()
        {
            var result = await _reportRepository.GetOverdueBooksAsync();
            return View(result.ValueOr(new List<OverdueBookReport>()));
        }

        public async Task<IActionResult> PopularBooks()
        {
            var result = await _reportRepository.GetPopularBooksAsync();
            return View(result.ValueOr(new List<PopularBookReport>()));
        }

        public async Task<IActionResult> ActiveSubscribers()
        {
            var result = await _reportRepository.GetActiveSubscribersAsync();
            return View(result.ValueOr(new List<ActiveSubscriberReport>()));
        }

        public async Task<IActionResult> ExpiringSubscriptions(int days = 30)
        {
            var result = await _reportRepository.GetExpiringSubscriptionsAsync(days);
            return View(result.ValueOr(new ExpiringSubscriptionsData { Days = days }));
        }

        public async Task<IActionResult> RentalHistory(Guid? subscriberId = null)
        {
            var result = await _reportRepository.GetRentalHistoryAsync(subscriberId);
            return View(result.ValueOr(new RentalHistoryData { SelectedSubscriberId = subscriberId }));
        }

        public async Task<IActionResult> BooksByCategory()
        {
            var result = await _reportRepository.GetBooksByCategoryAsync();
            return View(result.ValueOr(new List<BooksByCategoryReport>()));
        }

        public async Task<IActionResult> BooksByAuthor()
        {
            var result = await _reportRepository.GetBooksByAuthorAsync();
            return View(result.ValueOr(new List<BooksByAuthorReport>()));
        }

        public async Task<IActionResult> Books(Guid? authorId = null, Guid? categoryId = null, bool availableOnly = false)
        {
            var result = await _reportRepository.GetBooksAsync(new BookInventoryQuery
            {
                AuthorId = authorId,
                CategoryId = categoryId,
                AvailableOnly = availableOnly
            });

            return View(result.ValueOr(new BookInventoryReportData()));
        }
    }
}
