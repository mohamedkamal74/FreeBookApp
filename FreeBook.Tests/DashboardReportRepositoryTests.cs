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
    }

    [Fact]
    public async Task ReportRepository_AppliesFiltersAndBuildsRows()
    {
        using var context = CreateContext();
        var seed = await SeedAsync(context);
        var reports = CreateReportRepository(context);

        var booksResult = await reports.GetBooksAsync(new BookInventoryQuery
        {
            CategoryId = seed.Category.Id,
            AvailableOnly = true
        });
        Assert.True(booksResult.IsSuccess);
        var books = booksResult.Value;
        var row = Assert.Single(books.Rows);
        Assert.Equal(seed.Book.Name, row.Book.Name);
        Assert.Equal(seed.Author.Name, row.AuthorName);
        Assert.Equal(seed.Category.Name, row.CategoryName);
        Assert.Equal(2, row.CopyCount);
        Assert.Equal(1, row.AvailableCopyCount);
        Assert.Equal(2, row.RentalCount);
        Assert.True(books.Filter.AvailableOnly);
        Assert.Equal(seed.Category.Id, books.Filter.CategoryId);

        var expiringResult = await reports.GetExpiringSubscriptionsAsync(30);
        Assert.True(expiringResult.IsSuccess);
        Assert.Equal(30, expiringResult.Value.Days);
        Assert.Single(expiringResult.Value.Subscriptions);

        var historyResult = await reports.GetRentalHistoryAsync(seed.Subscriber.Id);
        Assert.True(historyResult.IsSuccess);
        Assert.Equal(2, historyResult.Value.Rentals.Count);
        Assert.Equal(2, historyResult.Value.Subscribers.Count);
        Assert.Equal(seed.Subscriber.Id, historyResult.Value.SelectedSubscriberId);

        var overdueResult = await reports.GetOverdueBooksAsync();
        Assert.True(overdueResult.IsSuccess);
        Assert.Single(overdueResult.Value);

        var popularResult = await reports.GetPopularBooksAsync();
        Assert.True(popularResult.IsSuccess);
        Assert.Equal(2, popularResult.Value.Count);
        Assert.Equal(2, popularResult.Value.First(item => item.Book.Id == seed.Book.Id).RentalCount);
    }

    [Fact]
    public void ValueOr_ReturnsFallbackForFailedResult()
    {
        var result = Result.Failure<List<string>>("ERROR", "Failed");

        Assert.Empty(result.ValueOr(new List<string>()));
    }

    private static FreeBookDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<FreeBookDbContext>()
            .UseInMemoryDatabase($"FreeBookDashboardTests_{Guid.NewGuid():N}")
            .Options);

    private static ServicesDashboard CreateDashboardRepository(FreeBookDbContext context) =>
        new(
            new ServicesBook(context, Mock.Of<ILogger<ServicesBook>>()),
            new ServicesBookCopy(context, Mock.Of<ILogger<ServicesBookCopy>>()),
            new ServicesSubscriber(context, Mock.Of<ILogger<ServicesSubscriber>>()),
            new ServicesSubscription(context, Mock.Of<ILogger<ServicesSubscription>>()),
            new ServicesRental(context, Mock.Of<ILogger<ServicesRental>>()),
            new ServicesAuthor(context, Mock.Of<ILogger<ServicesAuthor>>()),
            new ServicesCategory(context, Mock.Of<ILogger<ServicesCategory>>()),
            new ServicesGovernorate(context, Mock.Of<ILogger<ServicesGovernorate>>()),
            Mock.Of<ILogger<ServicesDashboard>>());

    private static ServicesReports CreateReportRepository(FreeBookDbContext context) =>
        new(
            new ServicesBook(context, Mock.Of<ILogger<ServicesBook>>()),
            new ServicesBookCopy(context, Mock.Of<ILogger<ServicesBookCopy>>()),
            new ServicesSubscriber(context, Mock.Of<ILogger<ServicesSubscriber>>()),
            new ServicesSubscription(context, Mock.Of<ILogger<ServicesSubscription>>()),
            new ServicesRental(context, Mock.Of<ILogger<ServicesRental>>()),
            new ServicesAuthor(context, Mock.Of<ILogger<ServicesAuthor>>()),
            new ServicesCategory(context, Mock.Of<ILogger<ServicesCategory>>()),
            Mock.Of<ILogger<ServicesReports>>());

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

        var book = new Book
        {
            Id = Guid.NewGuid(),
            Name = "Repository Book",
            AuthorId = author.Id,
            CategoryId = category.Id,
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

        return new SeedData(governorate, author, category, book, subscriber);
    }

    private sealed record SeedData(
        Governorate Governorate,
        Author Author,
        Category Category,
        Book Book,
        Subscriber Subscriber);
}
