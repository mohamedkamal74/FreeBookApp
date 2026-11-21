using Domin.Entity;
using Infrastructure.Common;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.Extensions.Logging;

namespace Infrastructure.IRepository.ServiceRepository
{
    public class ServicesDashboard : IDashboardRepository
    {
        private const int ExpiryWarningDays = 30;

        private readonly IServiceRepository<Book> _serviceBook;
        private readonly IServiceRepository<BookCopy> _serviceBookCopy;
        private readonly IServiceRepository<Subscriber> _serviceSubscriber;
        private readonly IServiceRepository<Subscription> _serviceSubscription;
        private readonly IServiceRepository<Rental> _serviceRental;
        private readonly IServiceRepository<Author> _serviceAuthor;
        private readonly IServiceRepository<Category> _serviceCategory;
        private readonly IServiceRepository<Governorate> _serviceGovernorate;
        private readonly ILogger<ServicesDashboard> _logger;

        public ServicesDashboard(
            IServiceRepository<Book> serviceBook,
            IServiceRepository<BookCopy> serviceBookCopy,
            IServiceRepository<Subscriber> serviceSubscriber,
            IServiceRepository<Subscription> serviceSubscription,
            IServiceRepository<Rental> serviceRental,
            IServiceRepository<Author> serviceAuthor,
            IServiceRepository<Category> serviceCategory,
            IServiceRepository<Governorate> serviceGovernorate,
            ILogger<ServicesDashboard> logger)
        {
            _serviceBook = serviceBook;
            _serviceBookCopy = serviceBookCopy;
            _serviceSubscriber = serviceSubscriber;
            _serviceSubscription = serviceSubscription;
            _serviceRental = serviceRental;
            _serviceAuthor = serviceAuthor;
            _serviceCategory = serviceCategory;
            _serviceGovernorate = serviceGovernorate;
            _logger = logger;
        }

        public async Task<Result<GeneralDashboardData>> GetGeneralAsync(GeneralDashboardQuery query)
        {
            try
            {
                var books = await AllAsync(_serviceBook.GetAllAsync());
                var copies = await AllAsync(_serviceBookCopy.GetAllAsync());
                var subscribers = await AllAsync(_serviceSubscriber.GetAllAsync());
                var subscriptions = await AllAsync(_serviceSubscription.GetAllAsync());
                var rentals = await AllAsync(_serviceRental.GetAllAsync());
                var authors = await AllAsync(_serviceAuthor.GetAllAsync());
                var categories = await AllAsync(_serviceCategory.GetAllAsync());

                var (rangeFrom, rangeTo) = ReportQueryHelpers.NormalizeRange(query.From, query.To);
                var now = DateTime.UtcNow;

                var filteredRentals = rentals
                    .Where(r => r.StartDate.Date >= rangeFrom && r.StartDate.Date <= rangeTo)
                    .Where(r => !query.AuthorId.HasValue || r.RentalCopies.Any(rc => rc.BookCopy?.Book?.AuthorId == query.AuthorId.Value))
                    .Where(r => !query.CategoryId.HasValue || r.RentalCopies.Any(rc => rc.BookCopy?.Book?.CategoryId == query.CategoryId.Value))
                    .ToList();

                var activeRentals = rentals.Where(r => r.RentalCopies.Any(rc => rc.ReturnDate == null)).ToList();
                var overdueRentals = activeRentals
                    .Where(r => r.RentalCopies.Any(rc => rc.ReturnDate == null && rc.EndDate < now))
                    .ToList();
                var expiringSubscriptions = subscriptions
                    .Where(s => s.EndDate >= now && s.EndDate <= now.AddDays(ExpiryWarningDays))
                    .ToList();

                var data = new GeneralDashboardData
                {
                    TotalBooks = books.Count,
                    TotalCopies = copies.Count,
                    AvailableCopies = copies.Count(c => c.IsAvailableForRental),
                    TotalSubscribers = subscribers.Count,
                    ActiveSubscriptions = subscriptions.Count(s => s.StartDate <= now && s.EndDate >= now),
                    ActiveRentals = activeRentals.Count,
                    OverdueRentals = overdueRentals.Count,
                    ExpiringSubscriptions = expiringSubscriptions.Count,
                    BlacklistedSubscribers = subscribers.Count(s => s.IsBlackListed),
                    RecentRentals = rentals.OrderByDescending(r => r.StartDate).Take(5).ToList(),
                    RecentSubscribers = subscribers
                        .OrderByDescending(s => s.CreatedOn)
                        .ThenBy(s => s.FirstName)
                        .ThenBy(s => s.LastName)
                        .Take(5)
                        .ToList(),
                    OverdueList = overdueRentals.Take(10).ToList(),
                    ExpiringSubs = expiringSubscriptions.Take(10).ToList(),
                    Filter = new DashboardFilterViewModel
                    {
                        From = rangeFrom,
                        To = rangeTo,
                        AuthorId = query.AuthorId,
                        CategoryId = query.CategoryId,
                        Authors = authors.OrderBy(a => a.Name, StringComparer.Ordinal).ToList(),
                        Categories = categories.OrderBy(c => c.Name, StringComparer.Ordinal).ToList()
                    },
                    Charts = BuildGeneralCharts(filteredRentals, books, copies, rangeFrom, rangeTo)
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
                var subscribers = await AllAsync(_serviceSubscriber.GetAllAsync());
                var subscriptions = await AllAsync(_serviceSubscription.GetAllAsync());
                var rentals = await AllAsync(_serviceRental.GetAllAsync());
                var governorates = await AllAsync(_serviceGovernorate.GetAllAsync());

                var (rangeFrom, rangeTo) = ReportQueryHelpers.NormalizeRange(query.From, query.To);
                var now = DateTime.UtcNow;

                var scopedSubscribers = query.GovernorateId.HasValue
                    ? subscribers.Where(s => s.GovernorateId == query.GovernorateId.Value).ToList()
                    : subscribers;
                var scopedIds = scopedSubscribers.Select(s => s.Id).ToHashSet();
                var scopedSubscriptions = subscriptions.Where(s => scopedIds.Contains(s.SubscriberId)).ToList();
                var scopedRentals = rentals.Where(r => scopedIds.Contains(r.SubscriberId)).ToList();

                var dashboard = new SubscribersDashboardViewModel
                {
                    TotalSubscribers = scopedSubscribers.Count,
                    ActiveSubscriptions = scopedSubscriptions.Count(s => s.StartDate <= now && s.EndDate >= now),
                    ExpiredSubscriptions = scopedSubscriptions.Count(s => s.EndDate < now),
                    ExpiringSoon = scopedSubscriptions.Count(s => s.EndDate >= now && s.EndDate <= now.AddDays(ExpiryWarningDays)),
                    Blacklisted = scopedSubscribers.Count(s => s.IsBlackListed),
                    WhatsAppEnabled = scopedSubscribers.Count(s => s.HasWhatsApp)
                };

                dashboard.ByGovernorate = scopedSubscribers
                    .GroupBy(s => s.Governorate?.Name ?? "—")
                    .Select(g => new NamedCount { Name = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ThenBy(x => x.Name, StringComparer.Ordinal)
                    .ToList();

                dashboard.AgeGroups = BuildAgeGroups(scopedSubscribers, now);
                dashboard.SubscriptionStatus = new List<NamedCount>
                {
                    new() { Name = "lbActive", Count = dashboard.ActiveSubscriptions },
                    new() { Name = "lbExpired", Count = dashboard.ExpiredSubscriptions }
                };
                dashboard.SubscriptionsPerMonth = ReportQueryHelpers.MonthlyBuckets(
                    scopedSubscriptions
                        .Where(s => s.StartDate.Date >= rangeFrom && s.StartDate.Date <= rangeTo)
                        .Select(s => s.StartDate),
                    rangeFrom,
                    rangeTo);

                var subscriberNames = subscribers.ToDictionary(s => s.Id, s => s.FullName);
                dashboard.TopRenters = scopedRentals
                    .Where(r => r.StartDate.Date >= rangeFrom && r.StartDate.Date <= rangeTo)
                    .GroupBy(r => r.SubscriberId)
                    .Select(g => new NamedCount
                    {
                        Name = subscriberNames.TryGetValue(g.Key, out var name) ? name : string.Empty,
                        Count = g.Count()
                    })
                    .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                    .OrderByDescending(x => x.Count)
                    .ThenBy(x => x.Name)
                    .Take(10)
                    .ToList();

                var data = new SubscribersDashboardData
                {
                    Dashboard = dashboard,
                    Filter = new SubscribersDashboardFilterViewModel
                    {
                        From = rangeFrom,
                        To = rangeTo,
                        GovernorateId = query.GovernorateId,
                        Governorates = governorates.OrderBy(g => g.Name, StringComparer.Ordinal).ToList()
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
                var books = await AllAsync(_serviceBook.GetAllAsync());
                var copies = await AllAsync(_serviceBookCopy.GetAllAsync());
                var rentals = await AllAsync(_serviceRental.GetAllAsync());
                var authors = await AllAsync(_serviceAuthor.GetAllAsync());
                var categories = await AllAsync(_serviceCategory.GetAllAsync());

                var (rangeFrom, rangeTo) = ReportQueryHelpers.NormalizeRange(query.From, query.To);

                var scopedBooks = books
                    .Where(b => !query.AuthorId.HasValue || b.AuthorId == query.AuthorId.Value)
                    .Where(b => !query.CategoryId.HasValue || b.CategoryId == query.CategoryId.Value)
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

                var dashboard = new BooksDashboardViewModel
                {
                    TotalBooks = scopedBooks.Count,
                    TotalCopies = scopedCopies.Count,
                    AvailableCopies = availableCopies,
                    RentedCopies = rentedCopies,
                    BooksWithoutCopies = scopedBooks.Count(b => !copyBookIds.Contains(b.Id))
                };

                dashboard.ByCategory = scopedBooks
                    .GroupBy(b => b.Category?.Name ?? "—")
                    .Select(g => new NamedCount { Name = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ThenBy(x => x.Name, StringComparer.Ordinal)
                    .ToList();

                dashboard.ByAuthor = scopedBooks
                    .GroupBy(b => b.Author?.Name ?? "—")
                    .Select(g => new NamedCount { Name = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ThenBy(x => x.Name, StringComparer.Ordinal)
                    .Take(10)
                    .ToList();

                dashboard.TopRentedBooks = rangedRentals
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

                dashboard.RentalsPerMonth = ReportQueryHelpers.MonthlyBuckets(
                    rangedRentals.Select(r => r.StartDate),
                    rangeFrom,
                    rangeTo);
                dashboard.Availability = new List<NamedCount>
                {
                    new() { Name = "lbAvailable", Count = availableCopies },
                    new() { Name = "lbRented", Count = rentedCopies }
                };

                var data = new BooksDashboardData
                {
                    Dashboard = dashboard,
                    Filter = new BooksDashboardFilterViewModel
                    {
                        From = rangeFrom,
                        To = rangeTo,
                        AuthorId = query.AuthorId,
                        CategoryId = query.CategoryId,
                        Authors = authors.OrderBy(a => a.Name, StringComparer.Ordinal).ToList(),
                        Categories = categories.OrderBy(c => c.Name, StringComparer.Ordinal).ToList()
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

        private static async Task<List<T>> AllAsync<T>(Task<Result<List<T>>> task)
        {
            var result = await task;
            return result.IsSuccess ? result.Value ?? new List<T>() : new List<T>();
        }

        private static DashboardChartsViewModel BuildGeneralCharts(
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

            charts.RentalsPerMonth = ReportQueryHelpers.MonthlyBuckets(
                rentals.Select(r => r.StartDate),
                from,
                to);

            return charts;
        }

        private static List<NamedCount> BuildAgeGroups(List<Subscriber> subscribers, DateTime now)
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
            foreach (var subscriber in subscribers)
            {
                var age = ReportQueryHelpers.AgeOn(subscriber.DateOfBirth, now);
                var bucket = age < 18 ? 0 : age <= 25 ? 1 : age <= 35 ? 2 : age <= 50 ? 3 : 4;
                counts[bucket]++;
            }

            return Enumerable.Range(0, keys.Length)
                .Select(i => new NamedCount { Name = keys[i], Count = counts[i] })
                .ToList();
        }
    }
}
