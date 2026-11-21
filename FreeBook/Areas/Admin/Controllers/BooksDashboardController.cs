using Domin.Entity;
using FreeBook.Models;
using FreeBook.Resources;
using Infrastructure.Common;
using Infrastructure.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace FreeBook.Areas.Admin.Controllers
{
    [Area(Helper.Admin)]
    [Authorize(Roles = "Admin,User")]
    public class BooksDashboardController : Controller
    {
        private const int DefaultChartMonths = 6;
        private const int MaxChartMonths = 12;

        private readonly IServiceRepository<Book> _serviceBook;
        private readonly IServiceRepository<BookCopy> _serviceBookCopy;
        private readonly IServiceRepository<Rental> _serviceRental;
        private readonly IServiceRepository<Author> _serviceAuthor;
        private readonly IServiceRepository<Category> _serviceCategory;
        private readonly IStringLocalizer<ResourceWeb> _localizer;

        public BooksDashboardController(
            IServiceRepository<Book> serviceBook,
            IServiceRepository<BookCopy> serviceBookCopy,
            IServiceRepository<Rental> serviceRental,
            IServiceRepository<Author> serviceAuthor,
            IServiceRepository<Category> serviceCategory,
            IStringLocalizer<ResourceWeb> localizer)
        {
            _serviceBook = serviceBook;
            _serviceBookCopy = serviceBookCopy;
            _serviceRental = serviceRental;
            _serviceAuthor = serviceAuthor;
            _serviceCategory = serviceCategory;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(DateTime? from = null, DateTime? to = null, Guid? authorId = null, Guid? categoryId = null)
        {
            var booksResult = await _serviceBook.GetAllAsync();
            var copiesResult = await _serviceBookCopy.GetAllAsync();
            var rentalsResult = await _serviceRental.GetAllAsync();
            var authorsResult = await _serviceAuthor.GetAllAsync();
            var categoriesResult = await _serviceCategory.GetAllAsync();

            var books = booksResult.IsSuccess ? booksResult.Value ?? new List<Book>() : new List<Book>();
            var copies = copiesResult.IsSuccess ? copiesResult.Value ?? new List<BookCopy>() : new List<BookCopy>();
            var rentals = rentalsResult.IsSuccess ? rentalsResult.Value ?? new List<Rental>() : new List<Rental>();
            var authors = authorsResult.IsSuccess ? authorsResult.Value ?? new List<Author>() : new List<Author>();
            var categories = categoriesResult.IsSuccess ? categoriesResult.Value ?? new List<Category>() : new List<Category>();

            // Same range semantics as the other dashboards: current month plus the five before it.
            var now = DateTime.UtcNow;
            var defaultTo = now.Date;
            var defaultFrom = new DateTime(defaultTo.Year, defaultTo.Month, 1).AddMonths(-(DefaultChartMonths - 1));
            var rangeFrom = (from ?? defaultFrom).Date;
            var rangeTo = (to ?? defaultTo).Date;
            if (rangeFrom > rangeTo)
            {
                (rangeFrom, rangeTo) = (rangeTo, rangeFrom);
            }

            var scopedBooks = books
                .Where(b => !authorId.HasValue || b.AuthorId == authorId.Value)
                .Where(b => !categoryId.HasValue || b.CategoryId == categoryId.Value)
                .ToList();
            var bookIds = scopedBooks.Select(b => b.Id).ToHashSet();
            var scopedCopies = copies.Where(c => bookIds.Contains(c.BookId)).ToList();
            var copyBookIds = scopedCopies.Select(c => c.BookId).ToHashSet();
            var rangedRentals = rentals
                .Where(r => r.StartDate.Date >= rangeFrom && r.StartDate.Date <= rangeTo)
                .Where(r => r.RentalCopies.Any(rc => rc.BookCopy != null && bookIds.Contains(rc.BookCopy.BookId)))
                .ToList();

            var availableCopies = scopedCopies.Count(c => c.IsAvailableForRental);
            var rentedCopies = scopedCopies.Count - availableCopies;

            var model = new BooksDashboardViewModel
            {
                TotalBooks = scopedBooks.Count,
                TotalCopies = scopedCopies.Count,
                AvailableCopies = availableCopies,
                RentedCopies = rentedCopies,
                BooksWithoutCopies = scopedBooks.Count(b => !copyBookIds.Contains(b.Id))
            };

            model.ByCategory = scopedBooks
                .GroupBy(b => b.Category?.Name ?? "—")
                .Select(g => new NamedCount { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Name, StringComparer.Ordinal)
                .ToList();

            model.ByAuthor = scopedBooks
                .GroupBy(b => b.Author?.Name ?? "—")
                .Select(g => new NamedCount { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Name, StringComparer.Ordinal)
                .Take(10)
                .ToList();

            model.TopRentedBooks = rangedRentals
                .SelectMany(r => r.RentalCopies)
                .Where(rc => rc.BookCopy != null && bookIds.Contains(rc.BookCopy.BookId))
                .GroupBy(rc => rc.BookCopy!.BookId)
                .Select(g => new NamedCount
                {
                    Name = g.First().BookCopy!.Book?.Name ?? string.Empty,
                    Count = g.Count()
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Name, StringComparer.Ordinal)
                .Take(10)
                .ToList();

            model.RentalsPerMonth = BuildMonthlyBuckets(rangedRentals, rangeFrom, rangeTo);
            model.Availability = new List<NamedCount>
            {
                new() { Name = _localizer["lbAvailable"].Value, Count = availableCopies },
                new() { Name = _localizer["lbRented"].Value, Count = rentedCopies }
            };

            ViewBag.Filter = new BooksDashboardFilterViewModel
            {
                From = rangeFrom,
                To = rangeTo,
                AuthorId = authorId,
                CategoryId = categoryId,
                Authors = authors.OrderBy(a => a.Name, StringComparer.Ordinal).ToList(),
                Categories = categories.OrderBy(c => c.Name, StringComparer.Ordinal).ToList()
            };
            ViewBag.Dashboard = model;

            return View();
        }

        private static List<NamedCount> BuildMonthlyBuckets(List<Rental> rentals, DateTime from, DateTime to)
        {
            var firstMonth = new DateTime(from.Year, from.Month, 1);
            var lastMonth = new DateTime(to.Year, to.Month, 1);
            var monthCount = (lastMonth.Year - firstMonth.Year) * 12 + lastMonth.Month - firstMonth.Month + 1;
            if (monthCount > MaxChartMonths)
            {
                monthCount = MaxChartMonths;
                firstMonth = lastMonth.AddMonths(-(MaxChartMonths - 1));
            }

            return Enumerable.Range(0, monthCount)
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
        }
    }
}
