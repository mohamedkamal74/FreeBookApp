using Domin.Entity;
using Infrastructure.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreeBook.Areas.Admin.Controllers
{
    [Area(Helper.Admin)]
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly IServiceRepository<Book> _serviceBook;
        private readonly IServiceRepository<BookCopy> _serviceBookCopy;
        private readonly IServiceRepository<Subscriber> _serviceSubscriber;
        private readonly IServiceRepository<Subscription> _serviceSubscription;
        private readonly IServiceRepository<Rental> _serviceRental;
        private readonly IServiceRepository<Author> _serviceAuthor;
        private readonly IServiceRepository<Category> _serviceCategory;

        public ReportsController(
            IServiceRepository<Book> serviceBook,
            IServiceRepository<BookCopy> serviceBookCopy,
            IServiceRepository<Subscriber> serviceSubscriber,
            IServiceRepository<Subscription> serviceSubscription,
            IServiceRepository<Rental> serviceRental,
            IServiceRepository<Author> serviceAuthor,
            IServiceRepository<Category> serviceCategory)
        {
            _serviceBook = serviceBook;
            _serviceBookCopy = serviceBookCopy;
            _serviceSubscriber = serviceSubscriber;
            _serviceSubscription = serviceSubscription;
            _serviceRental = serviceRental;
            _serviceAuthor = serviceAuthor;
            _serviceCategory = serviceCategory;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        public async Task<IActionResult> OverdueBooks()
        {
            var now = DateTime.UtcNow;
            var rentals = await _serviceRental.GetAllAsync();
            var overdueRentals = rentals
                .Where(r => r.RentalCopies.Any(rc => rc.ReturnDate == null && rc.EndDate < now))
                .OrderBy(r => r.StartDate)
                .ToList();
            return View(overdueRentals);
        }

        public async Task<IActionResult> PopularBooks()
        {
            var rentals = await _serviceRental.GetAllAsync();
            var popularBooks = rentals
                .SelectMany(r => r.RentalCopies)
                .GroupBy(rc => rc.BookCopy.BookId)
                .Select(g => new
                {
                    BookId = g.Key,
                    Book = g.First().BookCopy.Book,
                    RentalCount = g.Count()
                })
                .OrderByDescending(x => x.RentalCount)
                .Take(20)
                .ToList();
            return View(popularBooks);
        }

        public async Task<IActionResult> ActiveSubscribers()
        {
            var subscribers = await _serviceSubscriber.GetAllAsync();
            var now = DateTime.UtcNow;
            var activeSubs = subscribers
                .Where(s => s.Subscriptions.Any(sub => sub.StartDate <= now && sub.EndDate >= now))
                .OrderBy(s => s.FirstName)
                .ToList();
            return View(activeSubs);
        }

        public async Task<IActionResult> ExpiringSubscriptions(int days = 30)
        {
            var subscriptions = await _serviceSubscription.GetAllAsync();
            var now = DateTime.UtcNow;
            var future = now.AddDays(days);
            var expiring = subscriptions
                .Where(s => s.EndDate >= now && s.EndDate <= future)
                .OrderBy(s => s.EndDate)
                .ToList();
            ViewBag.Days = days;
            return View(expiring);
        }

        public async Task<IActionResult> RentalHistory(Guid? subscriberId = null)
        {
            var rentals = await _serviceRental.GetAllAsync();
            var subscribers = await _serviceSubscriber.GetAllAsync();

            if (subscriberId.HasValue)
            {
                rentals = rentals.Where(r => r.SubscriberId == subscriberId.Value).ToList();
            }

            ViewBag.Subscribers = subscribers;
            ViewBag.SelectedSubscriberId = subscriberId;
            return View(rentals.OrderByDescending(r => r.StartDate).ToList());
        }

        public async Task<IActionResult> BooksByCategory()
        {
            var books = await _serviceBook.GetAllAsync();
            var categories = await _serviceCategory.GetAllAsync();

            var booksByCategory = books
                .Where(b => b.CategoryId.HasValue)
                .GroupBy(b => b.CategoryId.Value)
                .Select(g => new
                {
                    Category = categories.FirstOrDefault(c => c.Id == g.Key),
                    Count = g.Count()
                })
                .Where(x => x.Category != null)
                .OrderByDescending(x => x.Count)
                .ToList();

            return View(booksByCategory);
        }

        public async Task<IActionResult> BooksByAuthor()
        {
            var books = await _serviceBook.GetAllAsync();
            var authors = await _serviceAuthor.GetAllAsync();

            var booksByAuthor = books
                .Where(b => b.AuthorId != Guid.Empty)
                .GroupBy(b => b.AuthorId)
                .Select(g => new
                {
                    Author = authors.FirstOrDefault(a => a.Id == g.Key),
                    Count = g.Count()
                })
                .Where(x => x.Author != null)
                .OrderByDescending(x => x.Count)
                .ToList();

            return View(booksByAuthor);
        }
    }
}
