using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.IRepository.ServiceRepository
{
    public class ServicesDashboard : IDashboardRepository
    {
        private const int ExpiryWarningDays = 30;

        private readonly FreeBookDbContext _context;
        private readonly ILogger<ServicesDashboard> _logger;

        public ServicesDashboard(FreeBookDbContext context, ILogger<ServicesDashboard> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Result<GeneralDashboardData>> GetGeneralAsync(GeneralDashboardQuery query)
        {
            try
            {
                var (rangeFrom, rangeTo) = ReportQueryHelpers.NormalizeRange(query.From, query.To);
                var toExclusive = rangeTo.AddDays(1);
                var now = DateTime.UtcNow;
                var warningTo = now.AddDays(ExpiryWarningDays);

                var totalBooks = await DashboardReportQueries.ActiveBooks(_context).CountAsync();
                var totalCopies = await DashboardReportQueries.BookCopies(_context).CountAsync();
                var availableCopies = await DashboardReportQueries.BookCopies(_context)
                    .CountAsync(copy => copy.IsAvailableForRental);
                var totalSubscribers = await _context.Subscribers.AsNoTracking().CountAsync();
                var activeSubscriptions = await _context.Subscriptions.AsNoTracking()
                    .CountAsync(subscription => subscription.StartDate <= now && subscription.EndDate >= now);
                var expiringSubscriptions = await _context.Subscriptions.AsNoTracking()
                    .CountAsync(subscription => subscription.EndDate >= now && subscription.EndDate <= warningTo);
                var blacklistedSubscribers = await _context.Subscribers.AsNoTracking()
                    .CountAsync(subscriber => subscriber.IsBlackListed);
                var activeRentals = await _context.Rentals.AsNoTracking()
                    .CountAsync(rental => rental.RentalCopies.Any(copy => copy.ReturnDate == null));
                var overdueRentals = await _context.Rentals.AsNoTracking()
                    .CountAsync(rental => rental.RentalCopies.Any(copy => copy.ReturnDate == null && copy.EndDate < now));

                var recentRentals = await DashboardReportQueries.RecentRentals(_context, 5).ToListAsync();
                var recentSubscribers = await DashboardReportQueries.RecentSubscribers(_context, 5).ToListAsync();
                var overdueList = await DashboardReportQueries.OverdueRentalRows(_context, now, 10).ToListAsync();
                var expiringSubs = await DashboardReportQueries
                    .ExpiringSubscriptionRows(_context, now, warningTo, 10)
                    .ToListAsync();

                var authors = await DashboardReportQueries.AuthorOptions(_context).ToListAsync();
                var categories = await DashboardReportQueries.CategoryOptions(_context).ToListAsync();

                var topRentedBooks = await DashboardReportQueries.TopRentedBooks(_context).ToListAsync();
                var topRenters = await DashboardReportQueries.TopRenters(_context).ToListAsync();
                var booksByCategory = await DashboardReportQueries.BooksByCategory(_context, query.AuthorId, query.CategoryId)
                    .ToListAsync();
                var rentalMonths = await DashboardReportQueries
                    .RentalMonths(_context, rangeFrom, toExclusive, query.AuthorId, query.CategoryId)
                    .ToListAsync();

                var data = new GeneralDashboardData
                {
                    TotalBooks = totalBooks,
                    TotalCopies = totalCopies,
                    AvailableCopies = availableCopies,
                    TotalSubscribers = totalSubscribers,
                    ActiveSubscriptions = activeSubscriptions,
                    ActiveRentals = activeRentals,
                    OverdueRentals = overdueRentals,
                    ExpiringSubscriptions = expiringSubscriptions,
                    BlacklistedSubscribers = blacklistedSubscribers,
                    RecentRentals = recentRentals,
                    RecentSubscribers = recentSubscribers,
                    OverdueList = overdueList,
                    ExpiringSubs = expiringSubs,
                    Filter = new DashboardFilterViewModel
                    {
                        From = rangeFrom,
                        To = rangeTo,
                        AuthorId = query.AuthorId,
                        CategoryId = query.CategoryId,
                        Authors = authors.OrderBy(author => author.Name, StringComparer.Ordinal).ToList(),
                        Categories = categories.OrderBy(category => category.Name, StringComparer.Ordinal).ToList()
                    },
                    Charts = BuildGeneralCharts(
                        availableCopies,
                        totalCopies - availableCopies,
                        topRentedBooks,
                        topRenters,
                        booksByCategory,
                        rentalMonths,
                        rangeFrom,
                        rangeTo)
                };

                return Result.Success(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building the general dashboard");
                return Result.Failure<GeneralDashboardData>("DASHBOARD_ERROR", "Failed to build the general dashboard", ex.Message);
            }
        }

        public async Task<Result<SubscribersDashboardData>> GetSubscribersAsync(SubscribersDashboardQuery query)
        {
            try
            {
                var (rangeFrom, rangeTo) = ReportQueryHelpers.NormalizeRange(query.From, query.To);
                var toExclusive = rangeTo.AddDays(1);
                var now = DateTime.UtcNow;
                var warningTo = now.AddDays(ExpiryWarningDays);
                var governorateId = query.GovernorateId;

                var scopedSubscribers = _context.Subscribers.AsNoTracking()
                    .Where(subscriber => !governorateId.HasValue || subscriber.GovernorateId == governorateId.Value);
                var scopedSubscriptions = _context.Subscriptions.AsNoTracking()
                    .Where(subscription => !governorateId.HasValue
                        || subscription.Subscriber.GovernorateId == governorateId.Value);

                var totalSubscribers = await scopedSubscribers.CountAsync();
                var blacklisted = await scopedSubscribers.CountAsync(subscriber => subscriber.IsBlackListed);
                var whatsAppEnabled = await scopedSubscribers.CountAsync(subscriber => subscriber.HasWhatsApp);
                var birthDates = await scopedSubscribers
                    .Select(subscriber => subscriber.DateOfBirth)
                    .ToListAsync();

                var activeSubscriptions = await scopedSubscriptions
                    .CountAsync(subscription => subscription.StartDate <= now && subscription.EndDate >= now);
                var expiredSubscriptions = await scopedSubscriptions
                    .CountAsync(subscription => subscription.EndDate < now);
                var expiringSoon = await scopedSubscriptions
                    .CountAsync(subscription => subscription.EndDate >= now && subscription.EndDate <= warningTo);

                var byGovernorate = await DashboardReportQueries.SubscribersByGovernorate(_context, governorateId)
                    .ToListAsync();
                var subscriptionsPerMonth = await DashboardReportQueries
                    .SubscriptionMonths(_context, rangeFrom, toExclusive, governorateId)
                    .ToListAsync();
                var topRenters = await DashboardReportQueries
                    .TopSubscriberRenters(_context, rangeFrom, toExclusive, governorateId)
                    .ToListAsync();
                var governorates = await DashboardReportQueries.GovernorateOptions(_context).ToListAsync();

                var dashboard = new SubscribersDashboardViewModel
                {
                    TotalSubscribers = totalSubscribers,
                    ActiveSubscriptions = activeSubscriptions,
                    ExpiredSubscriptions = expiredSubscriptions,
                    ExpiringSoon = expiringSoon,
                    Blacklisted = blacklisted,
                    WhatsAppEnabled = whatsAppEnabled,
                    ByGovernorate = byGovernorate
                        .OrderByDescending(row => row.Count)
                        .ThenBy(row => row.Name, StringComparer.Ordinal)
                        .ToList(),
                    AgeGroups = BuildAgeGroups(birthDates, now),
                    SubscriptionStatus = new List<NamedCount>
                    {
                        new() { Name = "lbActive", Count = activeSubscriptions },
                        new() { Name = "lbExpired", Count = expiredSubscriptions }
                    },
                    SubscriptionsPerMonth = ReportQueryHelpers.MonthlyBuckets(subscriptionsPerMonth, rangeFrom, rangeTo),
                    TopRenters = topRenters
                        .Where(row => !string.IsNullOrWhiteSpace(row.Name))
                        .OrderByDescending(row => row.Count)
                        .ThenBy(row => row.Name)
                        .Take(10)
                        .ToList()
                };

                var data = new SubscribersDashboardData
                {
                    Dashboard = dashboard,
                    Filter = new SubscribersDashboardFilterViewModel
                    {
                        From = rangeFrom,
                        To = rangeTo,
                        GovernorateId = governorateId,
                        Governorates = governorates
                            .OrderBy(governorate => governorate.Name, StringComparer.Ordinal)
                            .ToList()
                    }
                };

                return Result.Success(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building the subscribers dashboard");
                return Result.Failure<SubscribersDashboardData>("DASHBOARD_ERROR", "Failed to build the subscribers dashboard", ex.Message);
            }
        }

        public async Task<Result<BooksDashboardData>> GetBooksAsync(BooksDashboardQuery query)
        {
            try
            {
                var (rangeFrom, rangeTo) = ReportQueryHelpers.NormalizeRange(query.From, query.To);
                var toExclusive = rangeTo.AddDays(1);
                var authorId = query.AuthorId;
                var categoryId = query.CategoryId;

                var bookIds = await DashboardReportQueries.ActiveBooks(_context)
                    .Where(book => !authorId.HasValue || book.AuthorId == authorId.Value)
                    .Where(book => !categoryId.HasValue || book.CategoryId == categoryId.Value)
                    .Select(book => book.Id)
                    .ToListAsync();
                var copyBookIds = await DashboardReportQueries.BookCopies(_context)
                    .Where(copy => bookIds.Contains(copy.BookId))
                    .Select(copy => copy.BookId)
                    .Distinct()
                    .ToListAsync();
                var totalCopies = await DashboardReportQueries.BookCopies(_context)
                    .CountAsync(copy => bookIds.Contains(copy.BookId));
                var availableCopies = await DashboardReportQueries.BookCopies(_context)
                    .CountAsync(copy => bookIds.Contains(copy.BookId) && copy.IsAvailableForRental);

                var byCategory = await DashboardReportQueries.BooksByCategory(_context, authorId, categoryId)
                    .ToListAsync();
                var byAuthor = await DashboardReportQueries.BooksByAuthor(_context, authorId, categoryId)
                    .ToListAsync();
                var topRentedBooks = await DashboardReportQueries
                    .TopRentedBooksForBooks(_context, rangeFrom, toExclusive, bookIds)
                    .ToListAsync();
                var rentalsPerMonth = await DashboardReportQueries
                    .RentalMonthsForBooks(_context, rangeFrom, toExclusive, bookIds)
                    .ToListAsync();

                var authors = await DashboardReportQueries.AuthorOptions(_context).ToListAsync();
                var categories = await DashboardReportQueries.CategoryOptions(_context).ToListAsync();

                var copyLookup = copyBookIds.ToHashSet();
                var dashboard = new BooksDashboardViewModel
                {
                    TotalBooks = bookIds.Count,
                    TotalCopies = totalCopies,
                    AvailableCopies = availableCopies,
                    RentedCopies = totalCopies - availableCopies,
                    BooksWithoutCopies = bookIds.Count(id => !copyLookup.Contains(id)),
                    ByCategory = byCategory
                        .OrderByDescending(row => row.Count)
                        .ThenBy(row => row.Name, StringComparer.Ordinal)
                        .ToList(),
                    ByAuthor = byAuthor
                        .OrderByDescending(row => row.Count)
                        .ThenBy(row => row.Name, StringComparer.Ordinal)
                        .Take(10)
                        .ToList(),
                    TopRentedBooks = topRentedBooks
                        .Where(row => !string.IsNullOrWhiteSpace(row.Name))
                        .OrderByDescending(row => row.Count)
                        .ThenBy(row => row.Name)
                        .Take(10)
                        .ToList(),
                    RentalsPerMonth = ReportQueryHelpers.MonthlyBuckets(rentalsPerMonth, rangeFrom, rangeTo),
                    Availability = new List<NamedCount>
                    {
                        new() { Name = "lbAvailable", Count = availableCopies },
                        new() { Name = "lbRented", Count = totalCopies - availableCopies }
                    }
                };

                var data = new BooksDashboardData
                {
                    Dashboard = dashboard,
                    Filter = new BooksDashboardFilterViewModel
                    {
                        From = rangeFrom,
                        To = rangeTo,
                        AuthorId = authorId,
                        CategoryId = categoryId,
                        Authors = authors.OrderBy(author => author.Name, StringComparer.Ordinal).ToList(),
                        Categories = categories.OrderBy(category => category.Name, StringComparer.Ordinal).ToList()
                    }
                };

                return Result.Success(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building the books dashboard");
                return Result.Failure<BooksDashboardData>("DASHBOARD_ERROR", "Failed to build the books dashboard", ex.Message);
            }
        }

        private static DashboardChartsViewModel BuildGeneralCharts(
            int availableCopies,
            int rentedCopies,
            List<NamedCount> topRentedBooks,
            List<NamedCount> topRenters,
            List<NamedCount> booksByCategory,
            List<ReportQueryHelpers.MonthCount> rentalMonths,
            DateTime from,
            DateTime to)
        {
            var charts = new DashboardChartsViewModel
            {
                AvailableCopies = availableCopies,
                RentedCopies = rentedCopies,
                TopRentedBooks = topRentedBooks
                    .Where(row => !string.IsNullOrWhiteSpace(row.Name))
                    .OrderByDescending(row => row.Count)
                    .ThenBy(row => row.Name)
                    .Take(5)
                    .ToList(),
                TopRenters = topRenters
                    .Where(row => !string.IsNullOrWhiteSpace(row.Name))
                    .OrderByDescending(row => row.Count)
                    .ThenBy(row => row.Name)
                    .Take(5)
                    .ToList(),
                BooksByCategory = booksByCategory
                    .OrderByDescending(row => row.Count)
                    .ThenBy(row => row.Name)
                    .ToList(),
                RentalsPerMonth = ReportQueryHelpers.MonthlyBuckets(rentalMonths, from, to)
            };

            return charts;
        }

        private static List<NamedCount> BuildAgeGroups(List<DateTime> birthDates, DateTime now)
        {
            string[] keys =
            {
                "lbAgeUnder18",
                "lbAge18to25",
                "lbAge26to35",
                "lbAge36to50",
                "lbAgeOver50"
            };

            var counts = new int[keys.Length];
            foreach (var birthDate in birthDates)
            {
                var age = ReportQueryHelpers.AgeOn(birthDate, now);
                var bucket = age < 18 ? 0 : age <= 25 ? 1 : age <= 35 ? 2 : age <= 50 ? 3 : 4;
                counts[bucket]++;
            }

            return Enumerable.Range(0, keys.Length)
                .Select(i => new NamedCount { Name = keys[i], Count = counts[i] })
                .ToList();
        }
    }
}
