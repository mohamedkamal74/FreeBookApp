using Domin.Entity;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.IRepository.ServiceRepository
{
    public class ServicesReports : IReportRepository
    {
        private readonly FreeBookDbContext _context;
        private readonly ILogger<ServicesReports> _logger;

        public ServicesReports(FreeBookDbContext context, ILogger<ServicesReports> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Result<List<Rental>>> GetOverdueBooksAsync()
        {
            try
            {
                var overdue = await DashboardReportQueries.OverdueRentals(_context, DateTime.UtcNow)
                    .ToListAsync();
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
                var counts = await DashboardReportQueries.PopularBookCounts(_context)
                    .OrderByDescending(row => row.Count)
                    .Take(20)
                    .ToListAsync();
                var bookIds = counts.Select(row => row.Key).ToList();
                var books = await DashboardReportQueries.Books(_context)
                    .Where(book => bookIds.Contains(book.Id))
                    .ToListAsync();
                var bookLookup = books.ToDictionary(book => book.Id);

                var popular = counts
                    .Where(row => bookLookup.ContainsKey(row.Key))
                    .Select(row => new PopularBookReport
                    {
                        Book = bookLookup[row.Key],
                        RentalCount = row.Count
                    })
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
                var active = await DashboardReportQueries.ActiveSubscribers(_context, DateTime.UtcNow)
                    .ToListAsync();
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
                var subscriptions = await DashboardReportQueries
                    .ExpiringSubscriptions(_context, now, now.AddDays(days))
                    .ToListAsync();
                var data = new ExpiringSubscriptionsData
                {
                    Days = days,
                    Subscriptions = subscriptions
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
                var rentals = await DashboardReportQueries.RentalHistory(_context, subscriberId)
                    .ToListAsync();
                var subscribers = await DashboardReportQueries.RentalHistorySubscribers(_context)
                    .ToListAsync();

                var data = new RentalHistoryData
                {
                    Rentals = rentals,
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
                var counts = await DashboardReportQueries.BooksByCategoryCounts(_context)
                    .OrderByDescending(row => row.Count)
                    .ToListAsync();
                var categoryIds = counts.Select(row => row.Key).ToList();
                var categories = await DashboardReportQueries.ActiveCategories(_context)
                    .Where(category => categoryIds.Contains(category.Id))
                    .ToListAsync();
                var categoryLookup = categories.ToDictionary(category => category.Id);

                var rows = counts
                    .Where(row => categoryLookup.ContainsKey(row.Key))
                    .Select(row => new BooksByCategoryReport
                    {
                        Category = categoryLookup[row.Key],
                        Count = row.Count
                    })
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
                var counts = await DashboardReportQueries.BooksByAuthorCounts(_context)
                    .OrderByDescending(row => row.Count)
                    .ToListAsync();
                var authorIds = counts.Select(row => row.Key).ToList();
                var authors = await DashboardReportQueries.ActiveAuthors(_context)
                    .Where(author => authorIds.Contains(author.Id))
                    .ToListAsync();
                var authorLookup = authors.ToDictionary(author => author.Id);

                var rows = counts
                    .Where(row => authorLookup.ContainsKey(row.Key))
                    .Select(row => new BooksByAuthorReport
                    {
                        Author = authorLookup[row.Key],
                        Count = row.Count
                    })
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
                var rows = await DashboardReportQueries.BookInventoryRows(_context, query)
                    .ToListAsync();
                rows = rows
                    .OrderBy(row => row.Book.Name, StringComparer.Ordinal)
                    .ToList();

                var authors = await DashboardReportQueries.ActiveAuthors(_context)
                    .OrderBy(author => author.Name)
                    .ToListAsync();
                var categories = await DashboardReportQueries.ActiveCategories(_context)
                    .OrderBy(category => category.Name)
                    .ToListAsync();

                var data = new BookInventoryReportData
                {
                    Rows = rows,
                    Filter = new BookReportFilterViewModel
                    {
                        AuthorId = query.AuthorId,
                        CategoryId = query.CategoryId,
                        AvailableOnly = query.AvailableOnly,
                        Authors = authors.OrderBy(author => author.Name, StringComparer.Ordinal).ToList(),
                        Categories = categories.OrderBy(category => category.Name, StringComparer.Ordinal).ToList()
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
    }
}
