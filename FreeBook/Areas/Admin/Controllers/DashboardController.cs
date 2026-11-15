using Domin.Entity;
using FreeBook.Models;
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
        private const int DefaultChartMonths = 6;
        private const int MaxChartMonths = 12;

        private readonly IServiceRepository<Book> _serviceBook;
        private readonly IServiceRepository<BookCopy> _serviceBookCopy;
        private readonly IServiceRepository<Subscriber> _serviceSubscriber;
        private readonly IServiceRepository<Subscription> _serviceSubscription;
        private readonly IServiceRepository<Rental> _serviceRental;
        private readonly IServiceRepository<Author> _serviceAuthor;
        private readonly IServiceRepository<Category> _serviceCategory;

        public DashboardController(
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

        public async Task<IActionResult> Index(DateTime? from = null, DateTime? to = null, Guid? authorId = null, Guid? categoryId = null)
        {
            var booksResult = await _serviceBook.GetAllAsync();
            var copiesResult = await _serviceBookCopy.GetAllAsync();
            var subsResult = await _serviceSubscriber.GetAllAsync();
            var subsResult2 = await _serviceSubscription.GetAllAsync();
            var rentalsResult = await _serviceRental.GetAllAsync();
            var authorsResult = await _serviceAuthor.GetAllAsync();
            var categoriesResult = await _serviceCategory.GetAllAsync();

            var books = booksResult.IsSuccess ? booksResult.Value ?? new List<Book>() : new List<Book>();
            var copies = copiesResult.IsSuccess ? copiesResult.Value ?? new List<BookCopy>() : new List<BookCopy>();
            var subscribers = subsResult.IsSuccess ? subsResult.Value ?? new List<Subscriber>() : new List<Subscriber>();
            var subscriptions = subsResult2.IsSuccess ? subsResult2.Value ?? new List<Subscription>() : new List<Subscription>();
            var rentals = rentalsResult.IsSuccess ? rentalsResult.Value ?? new List<Rental>() : new List<Rental>();
            var authors = authorsResult.IsSuccess ? authorsResult.Value ?? new List<Author>() : new List<Author>();
            var categories = categoriesResult.IsSuccess ? categoriesResult.Value ?? new List<Category>() : new List<Category>();

            var now = DateTime.UtcNow;

            // Filter state: default to the current month plus the five before it.
            var defaultTo = now.Date;
            var defaultFrom = new DateTime(defaultTo.Year, defaultTo.Month, 1).AddMonths(-(DefaultChartMonths - 1));
            var rangeFrom = (from ?? defaultFrom).Date;
            var rangeTo = (to ?? defaultTo).Date;
            if (rangeFrom > rangeTo)
            {
                (rangeFrom, rangeTo) = (rangeTo, rangeFrom);
            }

            var filteredRentals = rentals
                .Where(r => r.StartDate.Date >= rangeFrom && r.StartDate.Date <= rangeTo)
                .Where(r => !authorId.HasValue || r.RentalCopies.Any(rc => rc.BookCopy?.Book?.AuthorId == authorId.Value))
                .Where(r => !categoryId.HasValue || r.RentalCopies.Any(rc => rc.BookCopy?.Book?.CategoryId == categoryId.Value))
                .ToList();

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

            ViewBag.Filter = new DashboardFilterViewModel
            {
                From = rangeFrom,
                To = rangeTo,
                AuthorId = authorId,
                CategoryId = categoryId,
                Authors = authors.OrderBy(a => a.Name, StringComparer.Ordinal).ToList(),
                Categories = categories.OrderBy(c => c.Name, StringComparer.Ordinal).ToList()
            };

            ViewBag.Charts = BuildCharts(filteredRentals, books, copies, rangeFrom, rangeTo);

            return View();
        }

        private static DashboardChartsViewModel BuildCharts(
            List<Rental> rentals,
            List<Book> books,
            List<BookCopy> copies,
            DateTime from,
            DateTime to)
        {
            var charts = new DashboardChartsViewModel
            {
                AvailableCopies = copies.Count(c => c.IsAvailableForRental),
                RentedCopies = copies.Count(c => !c.IsAvailableForRental)
            };

            charts.TopRentedBooks = rentals
                .SelectMany(r => r.RentalCopies)
                .Where(rc => rc.BookCopy != null)
                .GroupBy(rc => rc.BookCopy!.BookId)
                .Select(g => new NamedCount
                {
                    Name = g.First().BookCopy!.Book?.Name ?? string.Empty,
                    Count = g.Count()
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Name)
                .Take(5)
                .ToList();

            charts.TopRenters = rentals
                .GroupBy(r => r.SubscriberId)
                .Select(g => new NamedCount
                {
                    Name = g.First().Subscriber?.FullName ?? string.Empty,
                    Count = g.Count()
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Name)
                .Take(5)
                .ToList();

            charts.BooksByCategory = books
                .GroupBy(b => b.Category?.Name ?? "—")
                .Select(g => new NamedCount { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Name)
                .ToList();

            // Bucket the selected range by month, never more than a year of columns.
            var firstMonth = new DateTime(from.Year, from.Month, 1);
            var lastMonth = new DateTime(to.Year, to.Month, 1);
            var monthCount = (lastMonth.Year - firstMonth.Year) * 12 + lastMonth.Month - firstMonth.Month + 1;
            if (monthCount > MaxChartMonths)
            {
                monthCount = MaxChartMonths;
                firstMonth = lastMonth.AddMonths(-(MaxChartMonths - 1));
            }

            charts.RentalsPerMonth = Enumerable.Range(0, monthCount)
                .Select(i =>
                {
                    var month = firstMonth.AddMonths(i);
                    return new NamedCount
                    {
                        Name = month.ToString("MMM yyyy"),
                        Count = rentals.Count(r => r.StartDate.Year == month.Year && r.StartDate.Month == month.Month)
                    };
                })
                .ToList();

            return charts;
        }
    }
}
