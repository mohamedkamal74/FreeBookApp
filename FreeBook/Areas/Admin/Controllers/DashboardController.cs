using Domin.Entity;
using Infrastructure.Common;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreeBook.Areas.Admin.Controllers
{
    [Area(Helper.Admin)]
    [Authorize(Roles = "Admin,User")]
    public class DashboardController : Controller
    {
        private readonly IServiceRepository<Book> _serviceBook;
        private readonly IServiceRepository<BookCopy> _serviceBookCopy;
        private readonly IServiceRepository<Subscriber> _serviceSubscriber;
        private readonly IServiceRepository<Subscription> _serviceSubscription;
        private readonly IServiceRepository<Rental> _serviceRental;

        public DashboardController(
            IServiceRepository<Book> serviceBook,
            IServiceRepository<BookCopy> serviceBookCopy,
            IServiceRepository<Subscriber> serviceSubscriber,
            IServiceRepository<Subscription> serviceSubscription,
            IServiceRepository<Rental> serviceRental)
        {
            _serviceBook = serviceBook;
            _serviceBookCopy = serviceBookCopy;
            _serviceSubscriber = serviceSubscriber;
            _serviceSubscription = serviceSubscription;
            _serviceRental = serviceRental;
        }

        public async Task<IActionResult> Index()
        {
            var booksResult = await _serviceBook.GetAllAsync();
            var copiesResult = await _serviceBookCopy.GetAllAsync();
            var subsResult = await _serviceSubscriber.GetAllAsync();
            var subsResult2 = await _serviceSubscription.GetAllAsync();
            var rentalsResult = await _serviceRental.GetAllAsync();

            var books = booksResult.IsSuccess ? booksResult.Value ?? new List<Book>() : new List<Book>();
            var copies = copiesResult.IsSuccess ? copiesResult.Value ?? new List<BookCopy>() : new List<BookCopy>();
            var subscribers = subsResult.IsSuccess ? subsResult.Value ?? new List<Subscriber>() : new List<Subscriber>();
            var subscriptions = subsResult2.IsSuccess ? subsResult2.Value ?? new List<Subscription>() : new List<Subscription>();
            var rentals = rentalsResult.IsSuccess ? rentalsResult.Value ?? new List<Rental>() : new List<Rental>();

            var now = DateTime.UtcNow;
            var activeRentals = rentals.Where(r => r.RentalCopies.Any(rc => rc.ReturnDate == null)).ToList();
            var overdueRentals = activeRentals.Where(r => r.RentalCopies.Any(rc => rc.ReturnDate == null && rc.EndDate < now)).ToList();
            var expiringSubscriptions = subscriptions.Where(s => s.EndDate >= now && s.EndDate <= now.AddDays(30)).ToList();

            ViewBag.TotalBooks = books.Count;
            ViewBag.TotalCopies = copies.Count;
            ViewBag.AvailableCopies = copies.Count(c => c.IsAvailableForRental);
            ViewBag.TotalSubscribers = subscribers.Count;
            ViewBag.ActiveSubscriptions = subscriptions.Count(s => s.StartDate <= now && s.EndDate >= now);
            ViewBag.ActiveRentals = activeRentals.Count;
            ViewBag.OverdueRentals = overdueRentals.Count;
            ViewBag.ExpiringSubscriptions = expiringSubscriptions.Count;
            ViewBag.BlacklistedSubscribers = subscribers.Count(s => s.IsBlackListed);

            ViewBag.RecentRentals = rentals.OrderByDescending(r => r.StartDate).Take(5).ToList();
            ViewBag.RecentSubscribers = subscribers.OrderByDescending(s => s.Id).Take(5).ToList();
            ViewBag.OverdueList = overdueRentals.Take(10).ToList();
            ViewBag.ExpiringSubs = expiringSubscriptions.Take(10).ToList();

            return View();
        }
    }
}
