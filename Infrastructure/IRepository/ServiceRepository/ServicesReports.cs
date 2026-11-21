using Domin.Entity;
using Infrastructure.Common;
using Infrastructure.ViewModel;
using Microsoft.Extensions.Logging;

namespace Infrastructure.IRepository.ServiceRepository
{
    public class ServicesReports : IReportRepository
    {
        private readonly IServiceRepository<Book> _serviceBook;
        private readonly IServiceRepository<BookCopy> _serviceBookCopy;
        private readonly IServiceRepository<Subscriber> _serviceSubscriber;
        private readonly IServiceRepository<Subscription> _serviceSubscription;
        private readonly IServiceRepository<Rental> _serviceRental;
        private readonly IServiceRepository<Author> _serviceAuthor;
        private readonly IServiceRepository<Category> _serviceCategory;
        private readonly ILogger<ServicesReports> _logger;

        public ServicesReports(
            IServiceRepository<Book> serviceBook,
            IServiceRepository<BookCopy> serviceBookCopy,
            IServiceRepository<Subscriber> serviceSubscriber,
            IServiceRepository<Subscription> serviceSubscription,
            IServiceRepository<Rental> serviceRental,
            IServiceRepository<Author> serviceAuthor,
            IServiceRepository<Category> serviceCategory,
            ILogger<ServicesReports> logger)
        {
            _serviceBook = serviceBook;
            _serviceBookCopy = serviceBookCopy;
            _serviceSubscriber = serviceSubscriber;
            _serviceSubscription = serviceSubscription;
            _serviceRental = serviceRental;
            _serviceAuthor = serviceAuthor;
            _serviceCategory = serviceCategory;
            _logger = logger;
        }

        public async Task<Result<List<Rental>>> GetOverdueBooksAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var rentals = await AllAsync(_serviceRental.GetAllAsync());
                var overdue = rentals
                    .Where(r => r.RentalCopies.Any(rc => rc.ReturnDate == null && rc.EndDate < now))
                    .OrderBy(r => r.StartDate)
                    .ToList();
                return Result.Success(overdue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building the overdue-books report");
                return Result.Failure<List<Rental>>("REPORT_ERROR", "Failed to build the overdue-books report", ex.Message);
            }
        }

        public async Task<Result<List<PopularBookReport>>> GetPopularBooksAsync()
        {
            try
            {
                var rentals = await AllAsync(_serviceRental.GetAllAsync());
                var popular = rentals
                    .SelectMany(r => r.RentalCopies)
                    .GroupBy(rc => rc.BookCopy.BookId)
                    .Select(g => new PopularBookReport
                    {
                        Book = g.First().BookCopy.Book,
                        RentalCount = g.Count()
                    })
                    .OrderByDescending(x => x.RentalCount)
                    .Take(20)
                    .ToList();
                return Result.Success(popular);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building the popular-books report");
                return Result.Failure<List<PopularBookReport>>("REPORT_ERROR", "Failed to build the popular-books report", ex.Message);
            }
        }

        public async Task<Result<List<Subscriber>>> GetActiveSubscribersAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var subscribers = await AllAsync(_serviceSubscriber.GetAllAsync());
                var active = subscribers
                    .Where(s => s.Subscriptions.Any(sub => sub.StartDate <= now && sub.EndDate >= now))
                    .OrderBy(s => s.FirstName)
                    .ToList();
                return Result.Success(active);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building the active-subscribers report");
                return Result.Failure<List<Subscriber>>("REPORT_ERROR", "Failed to build the active-subscribers report", ex.Message);
            }
        }

        public async Task<Result<ExpiringSubscriptionsData>> GetExpiringSubscriptionsAsync(int days)
        {
            try
            {
                var now = DateTime.UtcNow;
                var future = now.AddDays(days);
                var subscriptions = await AllAsync(_serviceSubscription.GetAllAsync());
                var data = new ExpiringSubscriptionsData
                {
                    Days = days,
                    Subscriptions = subscriptions
                        .Where(s => s.EndDate >= now && s.EndDate <= future)
                        .OrderBy(s => s.EndDate)
                        .ToList()
                };
                return Result.Success(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building the expiring-subscriptions report");
                return Result.Failure<ExpiringSubscriptionsData>("REPORT_ERROR", "Failed to build the expiring-subscriptions report", ex.Message);
            }
        }

        public async Task<Result<RentalHistoryData>> GetRentalHistoryAsync(Guid? subscriberId)
        {
            try
            {
                var rentals = await AllAsync(_serviceRental.GetAllAsync());
                var subscribers = await AllAsync(_serviceSubscriber.GetAllAsync());

                if (subscriberId.HasValue)
                {
                    rentals = rentals.Where(r => r.SubscriberId == subscriberId.Value).ToList();
                }

                var data = new RentalHistoryData
                {
                    Rentals = rentals.OrderByDescending(r => r.StartDate).ToList(),
                    Subscribers = subscribers,
                    SelectedSubscriberId = subscriberId
                };
                return Result.Success(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building the rental-history report");
                return Result.Failure<RentalHistoryData>("REPORT_ERROR", "Failed to build the rental-history report", ex.Message);
            }
        }

        public async Task<Result<List<BooksByCategoryReport>>> GetBooksByCategoryAsync()
        {
            try
            {
                var books = await AllAsync(_serviceBook.GetAllAsync());
                var categories = await AllAsync(_serviceCategory.GetAllAsync());
                var rows = books
                    .Where(b => b.CategoryId.HasValue)
                    .GroupBy(b => b.CategoryId!.Value)
                    .Select(g => new BooksByCategoryReport
                    {
                        Category = categories.FirstOrDefault(c => c.Id == g.Key),
                        Count = g.Count()
                    })
                    .Where(x => x.Category != null)
                    .OrderByDescending(x => x.Count)
                    .ToList();
                return Result.Success(rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building the books-by-category report");
                return Result.Failure<List<BooksByCategoryReport>>("REPORT_ERROR", "Failed to build the books-by-category report", ex.Message);
            }
        }

        public async Task<Result<List<BooksByAuthorReport>>> GetBooksByAuthorAsync()
        {
            try
            {
                var books = await AllAsync(_serviceBook.GetAllAsync());
                var authors = await AllAsync(_serviceAuthor.GetAllAsync());
                var rows = books
                    .Where(b => b.AuthorId != Guid.Empty)
                    .GroupBy(b => b.AuthorId)
                    .Select(g => new BooksByAuthorReport
                    {
                        Author = authors.FirstOrDefault(a => a.Id == g.Key),
                        Count = g.Count()
                    })
                    .Where(x => x.Author != null)
                    .OrderByDescending(x => x.Count)
                    .ToList();
                return Result.Success(rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building the books-by-author report");
                return Result.Failure<List<BooksByAuthorReport>>("REPORT_ERROR", "Failed to build the books-by-author report", ex.Message);
            }
        }

        public async Task<Result<BookInventoryReportData>> GetBooksAsync(BookInventoryQuery query)
        {
            try
            {
                var books = await AllAsync(_serviceBook.GetAllAsync());
                var copies = await AllAsync(_serviceBookCopy.GetAllAsync());
                var rentals = await AllAsync(_serviceRental.GetAllAsync());
                var authors = await AllAsync(_serviceAuthor.GetAllAsync());
                var categories = await AllAsync(_serviceCategory.GetAllAsync());

                var copyCounts = copies.GroupBy(c => c.BookId).ToDictionary(g => g.Key, g => g.Count());
                var availableCounts = copies.Where(c => c.IsAvailableForRental)
                    .GroupBy(c => c.BookId).ToDictionary(g => g.Key, g => g.Count());
                var rentalCounts = rentals.SelectMany(r => r.RentalCopies)
                    .Where(rc => rc.BookCopy != null)
                    .GroupBy(rc => rc.BookCopy!.BookId)
                    .ToDictionary(g => g.Key, g => g.Count());

                var rows = books
                    .Where(b => !query.AuthorId.HasValue || b.AuthorId == query.AuthorId.Value)
                    .Where(b => !query.CategoryId.HasValue || b.CategoryId == query.CategoryId.Value)
                    .Where(b => !query.AvailableOnly || b.IsAvailableForRental)
                    .Select(b => new BookInventoryReport
                    {
                        Book = b,
                        AuthorName = b.Author?.Name,
                        CategoryName = b.Category?.Name,
                        SubCategoryName = b.SubCategory?.Name,
                        CopyCount = copyCounts.TryGetValue(b.Id, out var copyCount) ? copyCount : 0,
                        AvailableCopyCount = availableCounts.TryGetValue(b.Id, out var availableCount) ? availableCount : 0,
                        RentalCount = rentalCounts.TryGetValue(b.Id, out var rentalCount) ? rentalCount : 0
                    })
                    .OrderBy(x => x.Book.Name, StringComparer.Ordinal)
                    .ToList();

                var data = new BookInventoryReportData
                {
                    Rows = rows,
                    Filter = new BookReportFilterViewModel
                    {
                        AuthorId = query.AuthorId,
                        CategoryId = query.CategoryId,
                        AvailableOnly = query.AvailableOnly,
                        Authors = authors.OrderBy(a => a.Name, StringComparer.Ordinal).ToList(),
                        Categories = categories.OrderBy(c => c.Name, StringComparer.Ordinal).ToList()
                    }
                };

                return Result.Success(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building the books report");
                return Result.Failure<BookInventoryReportData>("REPORT_ERROR", "Failed to build the books report", ex.Message);
            }
        }

        private static async Task<List<T>> AllAsync<T>(Task<Result<List<T>>> task)
        {
            var result = await task;
            return result.IsSuccess ? result.Value ?? new List<T>() : new List<T>();
        }
    }
}
