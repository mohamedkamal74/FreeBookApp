using Domin.Entity;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.IRepository.ServiceRepository;
using Infrastructure.ViewModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace FreeBook.Tests;

public class DashboardReportRepositoryTests
{
    [Fact]
    public async Task DashboardRepositories_ComputeAggregatesAndKeepChartsLocalizationFree()
    {
        using var context = CreateContext();
        var seed = await SeedAsync(context);
        var dashboards = CreateDashboardRepository(context);
        var now = DateTime.UtcNow;

        var generalResult = await dashboards.GetGeneralAsync(new GeneralDashboardQuery
        {
            From = now.AddMonths(-1),
            To = now.AddDays(1),
            AuthorId = seed.Author.Id,
            CategoryId = seed.Category.Id
        });
        Assert.True(generalResult.IsSuccess);
        var general = generalResult.Value;

        Assert.Equal(2, general.TotalBooks);
        Assert.Equal(3, general.TotalCopies);
        Assert.Equal(2, general.AvailableCopies);
        Assert.Equal(2, general.TotalSubscribers);
        Assert.Equal(1, general.ActiveSubscriptions);
        Assert.Equal(2, general.ActiveRentals);
        Assert.Equal(1, general.OverdueRentals);
        Assert.Equal(1, general.ExpiringSubscriptions);
        Assert.Equal(seed.Author.Id, general.Filter.AuthorId);
        Assert.Equal(seed.Category.Id, general.Filter.CategoryId);
        Assert.Equal(2, general.Charts.RentalsPerMonth.Sum(row => row.Count));
        Assert.Contains(general.RecentRentals, row =>
            row.SubscriberName == "Repository Subscriber" && row.RentalCopyCount == 1);
        Assert.Contains(general.OverdueList, row =>
            row.SubscriberName == "Repository Subscriber" && row.BookName == seed.Book.Name && !row.PenaltyPaid);
        Assert.Contains(general.ExpiringSubs, row => row.SubscriberName == "Repository Subscriber");
        Assert.Contains(general.RecentSubscribers, row =>
            row.FullName == "Repository Subscriber" && row.AreaName == "Test Area" && row.GovernorateName == "Test Governorate");
        Assert.Contains(seed.Author.Id, general.Filter.Authors.Select(option => option.Id));
        Assert.Contains(seed.Category.Id, general.Filter.Categories.Select(option => option.Id));

        var subscribersResult = await dashboards.GetSubscribersAsync(new SubscribersDashboardQuery
        {
            From = now.AddMonths(-1),
            To = now.AddDays(1),
            GovernorateId = seed.Governorate.Id
        });
        Assert.True(subscribersResult.IsSuccess);
        var subscribers = subscribersResult.Value;

        Assert.Equal(1, subscribers.Dashboard.TotalSubscribers);
        Assert.Equal(1, subscribers.Dashboard.ActiveSubscriptions);
        Assert.Equal(0, subscribers.Dashboard.ExpiredSubscriptions);
        Assert.Equal(1, subscribers.Dashboard.ExpiringSoon);
        Assert.Equal(1, subscribers.Dashboard.WhatsAppEnabled);
        Assert.Equal(seed.Governorate.Id, subscribers.Filter.GovernorateId);
        Assert.Contains(seed.Governorate.Id, subscribers.Filter.Governorates.Select(option => option.Id));
        Assert.Equal(
            new[] { "lbAgeUnder18", "lbAge18to25", "lbAge26to35", "lbAge36to50", "lbAgeOver50" },
            subscribers.Dashboard.AgeGroups.Select(row => row.Name));
        Assert.Equal(
            new[] { "lbActive", "lbExpired" },
            subscribers.Dashboard.SubscriptionStatus.Select(row => row.Name));
        Assert.Equal(2, subscribers.Dashboard.TopRenters.Sum(row => row.Count));

        var booksResult = await dashboards.GetBooksAsync(new BooksDashboardQuery
        {
            From = now.AddMonths(-1),
            To = now.AddDays(1),
            AuthorId = seed.Author.Id
        });
        Assert.True(booksResult.IsSuccess);
        var books = booksResult.Value;

        Assert.Equal(1, books.Dashboard.TotalBooks);
        Assert.Equal(2, books.Dashboard.TotalCopies);
        Assert.Equal(1, books.Dashboard.AvailableCopies);
        Assert.Equal(1, books.Dashboard.RentedCopies);
        Assert.Equal(0, books.Dashboard.BooksWithoutCopies);
        Assert.Equal(new[] { "lbAvailable", "lbRented" }, books.Dashboard.Availability.Select(row => row.Name));
        Assert.Equal(seed.Author.Id, books.Filter.AuthorId);
        Assert.Contains(seed.Author.Id, books.Filter.Authors.Select(option => option.Id));
        Assert.Contains(seed.Category.Id, books.Filter.Categories.Select(option => option.Id));
    }

    [Fact]
    public async Task ReportRepository_AppliesFiltersAndBuildsRows()
    {
        using var context = CreateContext();
        var seed = await SeedAsync(context);
        var reports = CreateReportRepository(context);
        var now = DateTime.UtcNow;

        var booksResult = await reports.GetBooksAsync(new BookInventoryQuery
        {
            CategoryId = seed.Category.Id,
            AvailableOnly = true
        });
        Assert.True(booksResult.IsSuccess);
        var books = booksResult.Value;
        var row = Assert.Single(books.Rows);
        Assert.Equal(seed.Book.Name, row.BookName);
        Assert.Equal(seed.Author.Name, row.AuthorName);
        Assert.Equal(seed.Category.Name, row.CategoryName);
        Assert.Equal(seed.SubCategory.Name, row.SubCategoryName);
        Assert.Equal(2, row.CopyCount);
        Assert.Equal(1, row.AvailableCopyCount);
        Assert.Equal(2, row.RentalCount);
        Assert.True(books.Filter.AvailableOnly);
        Assert.Equal(seed.Category.Id, books.Filter.CategoryId);
        Assert.Contains(seed.Author.Id, books.Filter.Authors.Select(option => option.Id));
        Assert.Contains(seed.Category.Id, books.Filter.Categories.Select(option => option.Id));

        var activeResult = await reports.GetActiveSubscribersAsync();
        Assert.True(activeResult.IsSuccess);
        var active = Assert.Single(activeResult.Value);
        Assert.Equal(seed.Subscriber.Id, active.Id);
        Assert.Equal("Repository Subscriber", active.FullName);
        Assert.Equal("Test Governorate", active.GovernorateName);
        Assert.NotNull(active.ActiveSubscriptionEndDate);
        Assert.True(active.ActiveSubscriptionEndDate!.Value > now.AddDays(9));

        var expiringResult = await reports.GetExpiringSubscriptionsAsync(30);
        Assert.True(expiringResult.IsSuccess);
        Assert.Equal(30, expiringResult.Value.Days);
        var expiring = Assert.Single(expiringResult.Value.Subscriptions);
        Assert.Equal("Repository Subscriber", expiring.SubscriberName);
        Assert.Equal(seed.Subscriber.NationalId, expiring.NationalId);

        var historyResult = await reports.GetRentalHistoryAsync(seed.Subscriber.Id);
        Assert.True(historyResult.IsSuccess);
        Assert.Equal(2, historyResult.Value.Rentals.Count);
        Assert.Equal(2, historyResult.Value.Subscribers.Count);
        Assert.Equal(seed.Subscriber.Id, historyResult.Value.SelectedSubscriberId);
        Assert.All(historyResult.Value.Rentals, historyRow => Assert.NotNull(historyRow.BookName));
        Assert.Contains(seed.Subscriber.Id, historyResult.Value.Subscribers.Select(option => option.Id));

        var overdueResult = await reports.GetOverdueBooksAsync();
        Assert.True(overdueResult.IsSuccess);
        var overdue = Assert.Single(overdueResult.Value);
        Assert.Equal("Repository Subscriber", overdue.SubscriberName);
        Assert.Equal(seed.Book.Name, overdue.BookName);
        Assert.False(overdue.PenaltyPaid);

        var popularResult = await reports.GetPopularBooksAsync();
        Assert.True(popularResult.IsSuccess);
        Assert.Equal(2, popularResult.Value.Count);
        var popular = Assert.Single(popularResult.Value, item => item.BookId == seed.Book.Id);
        Assert.Equal(2, popular.RentalCount);
        Assert.Equal(seed.Author.Name, popular.AuthorName);
        Assert.Equal(seed.Category.Name, popular.CategoryName);

        var categoryResult = await reports.GetBooksByCategoryAsync();
        Assert.True(categoryResult.IsSuccess);
        Assert.Contains(categoryResult.Value, item =>
            item.CategoryName == seed.Category.Name && item.Count == 1);

        var authorResult = await reports.GetBooksByAuthorAsync();
        Assert.True(authorResult.IsSuccess);
        Assert.Contains(authorResult.Value, item =>
            item.AuthorName == seed.Author.Name && item.Count == 1);
    }

    [Fact]
    public void ValueOr_ReturnsFallbackForFailedResult()
    {
        var result = Result.Failure<List<string>>("ERROR", "Failed");

        Assert.Empty(result.ValueOr(new List<string>()));
    }

    [Fact]
    public void ReportDtos_DoNotExposeEntityTypes()
    {
        var entityTypes = typeof(Book).Assembly
            .GetTypes()
            .Where(type => type.Namespace == "Domin.Entity")
            .ToHashSet();
        var dtoTypes = new[]
        {
            typeof(OverdueBookReport),
            typeof(PopularBookReport),
            typeof(ActiveSubscriberReport),
            typeof(ExpiringSubscriptionReport),
            typeof(RentalHistoryReportRow),
            typeof(BooksByCategoryReport),
            typeof(BooksByAuthorReport),
            typeof(ReportFilterOption),
            typeof(BookInventoryReport),
            typeof(BookReportFilterViewModel),
            typeof(BookInventoryReportData),
            typeof(RentalHistoryData),
            typeof(ExpiringSubscriptionsData)
        };

        foreach (var dtoType in dtoTypes)
        {
            foreach (var property in dtoType.GetProperties())
            {
                var propertyType = property.PropertyType;
                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    propertyType = propertyType.GetGenericArguments()[0];
                }

                Assert.DoesNotContain(propertyType, entityTypes);
            }
        }
    }

    [Fact]
    public void DashboardDtos_DoNotExposeEntityTypes()
    {
        var entityTypes = typeof(Book).Assembly
            .GetTypes()
            .Where(type => type.Namespace == "Domin.Entity")
            .ToHashSet();
        var dtoTypes = new[]
        {
            typeof(RecentRentalRow),
            typeof(OverdueRentalRow),
            typeof(ExpiringSubscriptionRow),
            typeof(RecentSubscriberRow),
            typeof(GeneralDashboardData),
            typeof(DashboardFilterViewModel),
            typeof(DashboardChartsViewModel),
            typeof(SubscribersDashboardData),
            typeof(SubscribersDashboardFilterViewModel),
            typeof(SubscribersDashboardViewModel),
            typeof(BooksDashboardData),
            typeof(BooksDashboardFilterViewModel),
            typeof(BooksDashboardViewModel),
            typeof(NamedCount),
            typeof(ReportFilterOption)
        };

        foreach (var dtoType in dtoTypes)
        {
            foreach (var property in dtoType.GetProperties())
            {
                var propertyType = property.PropertyType;
                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    propertyType = propertyType.GetGenericArguments()[0];
                }

                Assert.DoesNotContain(propertyType, entityTypes);
            }
        }
    }

    [Fact]
    public void MonthlyBuckets_CapsAtTwelveMonthsAndUsesGroupedCounts()
    {
        var counts = new[]
        {
            new ReportQueryHelpers.MonthCount { Year = 2025, Month = 1, Count = 2 },
            new ReportQueryHelpers.MonthCount { Year = 2026, Month = 3, Count = 4 }
        };

        var rows = ReportQueryHelpers.MonthlyBuckets(
            counts,
            new DateTime(2025, 1, 1),
            new DateTime(2026, 3, 1));

        Assert.Equal(ReportQueryHelpers.MaxChartMonths, rows.Count);
        Assert.Equal(4, rows.Sum(row => row.Count));
    }

    [Fact]
    public void DashboardReportQueries_TranslateToSqlServer()
    {
        using var context = CreateSqlServerQueryContext();
        var from = DateTime.UtcNow.AddMonths(-1);
        var toExclusive = DateTime.UtcNow.AddDays(1);
        var bookIds = new List<Guid> { Guid.NewGuid() };
        var authorId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var governorateId = Guid.NewGuid();

        AssertSql(DashboardReportQueries.RecentRentals(context, 5));
        AssertSql(DashboardReportQueries.RecentSubscribers(context, 5));
        AssertSql(DashboardReportQueries.OverdueRentalRows(context, from, 10));
        AssertSql(DashboardReportQueries.ExpiringSubscriptionRows(context, from, toExclusive, 10));
        AssertSql(DashboardReportQueries.AuthorOptions(context));
        AssertSql(DashboardReportQueries.CategoryOptions(context));
        AssertSql(DashboardReportQueries.GovernorateOptions(context));
        AssertSql(DashboardReportQueries.TopRentedBooks(context));
        AssertSql(DashboardReportQueries.TopRenters(context));
        AssertSql(DashboardReportQueries.BooksByCategory(context, authorId, categoryId));
        AssertSql(DashboardReportQueries.SubscribersByGovernorate(context, governorateId));
        AssertSql(DashboardReportQueries.TopSubscriberRenters(context, from, toExclusive, governorateId));
        AssertSql(DashboardReportQueries.RentalMonths(context, from, toExclusive, authorId, categoryId));
        AssertSql(DashboardReportQueries.SubscriptionMonths(context, from, toExclusive, governorateId));
        AssertSql(DashboardReportQueries.TopRentedBooksForBooks(context, from, toExclusive, bookIds));
        AssertSql(DashboardReportQueries.RentalMonthsForBooks(context, from, toExclusive, bookIds));
        AssertSql(ReportQueries.OverdueBooks(context, from));
        AssertSql(ReportQueries.PopularBooks(context));
        AssertSql(ReportQueries.ActiveSubscribers(context, from));
        AssertSql(ReportQueries.ExpiringSubscriptions(context, from, toExclusive));
        AssertSql(ReportQueries.RentalHistoryRows(context, null));
        AssertSql(ReportQueries.RentalHistoryOptions(context));
        AssertSql(ReportQueries.BooksByCategoryRows(context));
        AssertSql(ReportQueries.BooksByAuthorRows(context));
        AssertSql(ReportQueries.BookInventoryRows(context, new BookInventoryQuery
        {
            AuthorId = authorId,
            CategoryId = categoryId,
            AvailableOnly = true
        }));
        AssertSql(ReportQueries.AuthorOptions(context));
        AssertSql(ReportQueries.CategoryOptions(context));
    }

    private static FreeBookDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<FreeBookDbContext>()
            .UseInMemoryDatabase($"FreeBookDashboardTests_{Guid.NewGuid():N}")
            .Options);

    private static FreeBookDbContext CreateSqlServerQueryContext() =>
        new(new DbContextOptionsBuilder<FreeBookDbContext>()
            .UseSqlServer("Server=localhost;Database=FreeBookQueryTranslation;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options);

    private static void AssertSql(IQueryable query)
    {
        var sql = query.ToQueryString();
        Assert.False(string.IsNullOrWhiteSpace(sql));
        Assert.Contains("SELECT", sql, StringComparison.OrdinalIgnoreCase);
    }

    private static ServicesDashboard CreateDashboardRepository(FreeBookDbContext context) =>
        new(context, Mock.Of<ILogger<ServicesDashboard>>());

    private static ServicesReports CreateReportRepository(FreeBookDbContext context) =>
        new(context, Mock.Of<ILogger<ServicesReports>>());

    private static async Task<SeedData> SeedAsync(FreeBookDbContext context)
    {
        var now = DateTime.UtcNow;
        var governorate = new Governorate { Id = Guid.NewGuid(), Name = "Test Governorate" };
        var otherGovernorate = new Governorate { Id = Guid.NewGuid(), Name = "Other Governorate" };
        var area = new Area { Id = Guid.NewGuid(), Name = "Test Area", GovernorateId = governorate.Id };
        var otherArea = new Area { Id = Guid.NewGuid(), Name = "Other Area", GovernorateId = otherGovernorate.Id };
        var author = new Author { Id = Guid.NewGuid(), Name = "Repository Author" };
        var otherAuthor = new Author { Id = Guid.NewGuid(), Name = "Other Author" };
        var category = new Category { Id = Guid.NewGuid(), Name = "Repository Category" };
        var otherCategory = new Category { Id = Guid.NewGuid(), Name = "Other Category" };
        var subCategory = new SubCategory
        {
            Id = Guid.NewGuid(),
            Name = "Repository Subcategory",
            CategoryId = category.Id,
            Currentstate = (int)Helper.ECurrentState.Active
        };

        var book = new Book
        {
            Id = Guid.NewGuid(),
            Name = "Repository Book",
            AuthorId = author.Id,
            CategoryId = category.Id,
            SubCategoryId = subCategory.Id,
            IsAvailableForRental = true
        };
        var otherBook = new Book
        {
            Id = Guid.NewGuid(),
            Name = "Other Book",
            AuthorId = otherAuthor.Id,
            CategoryId = otherCategory.Id,
            IsAvailableForRental = true
        };

        var availableCopy = new BookCopy
        {
            Id = Guid.NewGuid(),
            BookId = book.Id,
            EditionNumber = 1,
            SerialNumber = 1,
            IsAvailableForRental = true
        };
        var rentedCopy = new BookCopy
        {
            Id = Guid.NewGuid(),
            BookId = book.Id,
            EditionNumber = 2,
            SerialNumber = 2,
            IsAvailableForRental = false
        };
        var otherCopy = new BookCopy
        {
            Id = Guid.NewGuid(),
            BookId = otherBook.Id,
            EditionNumber = 1,
            SerialNumber = 3,
            IsAvailableForRental = true
        };

        var subscriber = new Subscriber
        {
            Id = Guid.NewGuid(),
            FirstName = "Repository",
            LastName = "Subscriber",
            DateOfBirth = now.AddYears(-25),
            NationalId = "12345678901",
            MobileNumber = "01000000001",
            Email = "repository-subscriber@test.local",
            GovernorateId = governorate.Id,
            AreaId = area.Id,
            HasWhatsApp = true,
            CreatedOn = now.AddDays(-3)
        };
        var otherSubscriber = new Subscriber
        {
            Id = Guid.NewGuid(),
            FirstName = "Other",
            LastName = "Subscriber",
            DateOfBirth = now.AddYears(-40),
            NationalId = "12345678902",
            MobileNumber = "01000000002",
            Email = "other-subscriber@test.local",
            GovernorateId = otherGovernorate.Id,
            AreaId = otherArea.Id,
            CreatedOn = now.AddDays(-2)
        };

        var activeSubscription = new Subscription
        {
            Id = Guid.NewGuid(),
            SubscriberId = subscriber.Id,
            StartDate = now.AddDays(-1),
            EndDate = now.AddDays(10)
        };
        var expiredSubscription = new Subscription
        {
            Id = Guid.NewGuid(),
            SubscriberId = otherSubscriber.Id,
            StartDate = now.AddDays(-100),
            EndDate = now.AddDays(-1)
        };

        var overdueRental = new Rental
        {
            Id = Guid.NewGuid(),
            SubscriberId = subscriber.Id,
            StartDate = now.AddDays(-2),
            RentalCopies = new List<RentalCopy>
            {
                new()
                {
                    BookCopyId = availableCopy.Id,
                    BookCopy = availableCopy,
                    RentalDate = now.AddDays(-2),
                    EndDate = now.AddDays(-1)
                }
            }
        };
        var activeRental = new Rental
        {
            Id = Guid.NewGuid(),
            SubscriberId = subscriber.Id,
            StartDate = now.AddDays(-1),
            RentalCopies = new List<RentalCopy>
            {
                new()
                {
                    BookCopyId = rentedCopy.Id,
                    BookCopy = rentedCopy,
                    RentalDate = now.AddDays(-1),
                    EndDate = now.AddDays(5)
                }
            }
        };
        var returnedRental = new Rental
        {
            Id = Guid.NewGuid(),
            SubscriberId = otherSubscriber.Id,
            StartDate = now.AddDays(-1),
            RentalCopies = new List<RentalCopy>
            {
                new()
                {
                    BookCopyId = otherCopy.Id,
                    BookCopy = otherCopy,
                    RentalDate = now.AddDays(-1),
                    EndDate = now.AddDays(1),
                    ReturnDate = now
                }
            }
        };

        context.AddRange(
            governorate,
            otherGovernorate,
            area,
            otherArea,
            author,
            otherAuthor,
            category,
            otherCategory,
            subCategory,
            book,
            otherBook,
            availableCopy,
            rentedCopy,
            otherCopy,
            subscriber,
            otherSubscriber,
            activeSubscription,
            expiredSubscription,
            overdueRental,
            activeRental,
            returnedRental);
        await context.SaveChangesAsync();

        return new SeedData(governorate, author, category, subCategory, book, subscriber);
    }

    private sealed record SeedData(
        Governorate Governorate,
        Author Author,
        Category Category,
        SubCategory SubCategory,
        Book Book,
        Subscriber Subscriber);
}
