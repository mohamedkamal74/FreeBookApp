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

        public async Task<Result<List<OverdueBookReport>>> GetOverdueBooksAsync()
        {
            try
            {
                var rows = await ReportQueries.OverdueBooks(_context, DateTime.UtcNow)
                    .ToListAsync();
                return Result.Success(rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building the overdue-books report");
                return Result.Failure<List<OverdueBookReport>>("REPORT_ERROR", "Failed to build the overdue-books report", ex.Message);
            }
        }

        public async Task<Result<List<PopularBookReport>>> GetPopularBooksAsync()
        {
            try
            {
                var rows = await ReportQueries.PopularBooks(_context)
                    .OrderByDescending(row => row.RentalCount)
                    .Take(20)
                    .ToListAsync();
                return Result.Success(rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building the popular-books report");
                return Result.Failure<List<PopularBookReport>>("REPORT_ERROR", "Failed to build the popular-books report", ex.Message);
            }
        }

        public async Task<Result<List<ActiveSubscriberReport>>> GetActiveSubscribersAsync()
        {
            try
            {
                var rows = await ReportQueries.ActiveSubscribers(_context, DateTime.UtcNow)
                    .ToListAsync();
                return Result.Success(rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building the active-subscribers report");
                return Result.Failure<List<ActiveSubscriberReport>>("REPORT_ERROR", "Failed to build the active-subscribers report", ex.Message);
            }
        }

        public async Task<Result<ExpiringSubscriptionsData>> GetExpiringSubscriptionsAsync(int days)
        {
            try
            {
                var now = DateTime.UtcNow;
                var rows = await ReportQueries.ExpiringSubscriptions(_context, now, now.AddDays(days))
                    .ToListAsync();
                var data = new ExpiringSubscriptionsData
                {
                    Days = days,
                    Subscriptions = rows
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
                var rows = await ReportQueries.RentalHistoryRows(_context, subscriberId)
                    .ToListAsync();
                var subscribers = await ReportQueries.RentalHistoryOptions(_context)
                    .ToListAsync();

                var data = new RentalHistoryData
                {
                    Rentals = rows,
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
                var rows = await ReportQueries.BooksByCategoryRows(_context)
                    .OrderByDescending(row => row.Count)
                    .ToListAsync();
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
                var rows = await ReportQueries.BooksByAuthorRows(_context)
                    .OrderByDescending(row => row.Count)
                    .ToListAsync();
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
                var rows = await ReportQueries.BookInventoryRows(_context, query)
                    .ToListAsync();
                rows = rows
                    .OrderBy(row => row.BookName, StringComparer.Ordinal)
                    .ToList();

                var authors = await ReportQueries.AuthorOptions(_context).ToListAsync();
                var categories = await ReportQueries.CategoryOptions(_context).ToListAsync();

                var data = new BookInventoryReportData
                {
                    Rows = rows,
                    Filter = new BookReportFilterViewModel
                    {
                        AuthorId = query.AuthorId,
                        CategoryId = query.CategoryId,
                        AvailableOnly = query.AvailableOnly,
                        Authors = authors,
                        Categories = categories
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
