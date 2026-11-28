using System;
using System.Threading.Tasks;
using Domin.Entity;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.IRepository.ServiceRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace FreeBook.Tests;

public class FilteredListRepositoryTests
{
    [Fact]
    public async Task GetFilteredAsync_AppliesFiltersInsideRepositories()
    {
        using var context = new FreeBookDbContext(new DbContextOptionsBuilder<FreeBookDbContext>()
            .UseInMemoryDatabase($"FreeBookFilteredLists_{Guid.NewGuid():N}")
            .Options);
        var now = DateTime.UtcNow;
        await SeedAsync(context, now);

        var subscribers = new ServicesSubscriber(context, Mock.Of<ILogger<ServicesSubscriber>>());
        var subscriptions = new ServicesSubscription(context, Mock.Of<ILogger<ServicesSubscription>>());
        var rentals = new ServicesRental(context, Mock.Of<ILogger<ServicesRental>>());

        var allSubscribers = await subscribers.GetAllAsync();
        var blacklistedSubscribers = await subscribers.GetFilteredAsync(ListFilters.Blacklisted);
        Assert.True(allSubscribers.IsSuccess);
        Assert.True(blacklistedSubscribers.IsSuccess);
        Assert.Equal(3, allSubscribers.Value!.Count);
        Assert.Single(blacklistedSubscribers.Value!);
        Assert.Equal("ZBlacklisted", blacklistedSubscribers.Value![0].FirstName);

        var allSubscriptions = await subscriptions.GetAllAsync();
        var activeSubscriptions = await subscriptions.GetFilteredAsync(ListFilters.Active);
        var expiringSubscriptions = await subscriptions.GetFilteredAsync(ListFilters.Expiring);
        Assert.True(allSubscriptions.IsSuccess);
        Assert.True(activeSubscriptions.IsSuccess);
        Assert.True(expiringSubscriptions.IsSuccess);
        Assert.Equal(3, allSubscriptions.Value!.Count);
        Assert.Equal(2, activeSubscriptions.Value!.Count);
        Assert.Single(expiringSubscriptions.Value!);

        var allRentals = await rentals.GetAllAsync();
        var activeRentals = await rentals.GetFilteredAsync(ListFilters.Active);
        var overdueRentals = await rentals.GetFilteredAsync(ListFilters.Overdue);
        Assert.True(allRentals.IsSuccess);
        Assert.True(activeRentals.IsSuccess);
        Assert.True(overdueRentals.IsSuccess);
        Assert.Equal(3, allRentals.Value!.Count);
        Assert.Equal(2, activeRentals.Value!.Count);
        Assert.Single(overdueRentals.Value!);
    }

    private static async Task SeedAsync(FreeBookDbContext context, DateTime now)
    {
        var governorate = new Governorate { Id = Guid.NewGuid(), Name = "Filter Governorate" };
        var area = new Area
        {
            Id = Guid.NewGuid(),
            Name = "Filter Area",
            GovernorateId = governorate.Id
        };
        var book = new Book
        {
            Id = Guid.NewGuid(),
            Name = "Filter Book",
            AuthorId = Guid.NewGuid(),
            IsAvailableForRental = true
        };

        var blacklisted = CreateSubscriber("ZBlacklisted", governorate.Id, area.Id, now, true);
        var active = CreateSubscriber("AActive", governorate.Id, area.Id, now, false);
        var expiring = CreateSubscriber("BExpiring", governorate.Id, area.Id, now, false);
        context.AddRange(governorate, area, book, blacklisted, active, expiring);

        context.AddRange(
            new Subscription
            {
                Id = Guid.NewGuid(),
                SubscriberId = blacklisted.Id,
                StartDate = now.AddDays(-100),
                EndDate = now.AddDays(-1)
            },
            new Subscription
            {
                Id = Guid.NewGuid(),
                SubscriberId = active.Id,
                StartDate = now.AddDays(-1),
                EndDate = now.AddDays(100)
            },
            new Subscription
            {
                Id = Guid.NewGuid(),
                SubscriberId = expiring.Id,
                StartDate = now.AddDays(-1),
                EndDate = now.AddDays(10)
            });

        var activeCopy = CreateBookCopy(book.Id, 1);
        var overdueCopy = CreateBookCopy(book.Id, 2);
        var returnedCopy = CreateBookCopy(book.Id, 3);
        context.AddRange(activeCopy, overdueCopy, returnedCopy);

        var activeRental = new Rental
        {
            Id = Guid.NewGuid(),
            SubscriberId = active.Id,
            StartDate = now.AddDays(-1)
        };
        activeRental.RentalCopies.Add(new RentalCopy
        {
            RentalId = activeRental.Id,
            BookCopyId = activeCopy.Id,
            RentalDate = now.AddDays(-1),
            EndDate = now.AddDays(5)
        });

        var overdueRental = new Rental
        {
            Id = Guid.NewGuid(),
            SubscriberId = expiring.Id,
            StartDate = now.AddDays(-10)
        };
        overdueRental.RentalCopies.Add(new RentalCopy
        {
            RentalId = overdueRental.Id,
            BookCopyId = overdueCopy.Id,
            RentalDate = now.AddDays(-10),
            EndDate = now.AddDays(-1)
        });

        var returnedRental = new Rental
        {
            Id = Guid.NewGuid(),
            SubscriberId = blacklisted.Id,
            StartDate = now.AddDays(-20)
        };
        returnedRental.RentalCopies.Add(new RentalCopy
        {
            RentalId = returnedRental.Id,
            BookCopyId = returnedCopy.Id,
            RentalDate = now.AddDays(-20),
            EndDate = now.AddDays(-10),
            ReturnDate = now.AddDays(-9)
        });

        context.AddRange(activeRental, overdueRental, returnedRental);
        await context.SaveChangesAsync();
    }

    private static Subscriber CreateSubscriber(string firstName, Guid governorateId, Guid areaId, DateTime now, bool blacklisted) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = firstName,
        LastName = "Filter",
        DateOfBirth = now.AddYears(-30),
        NationalId = Guid.NewGuid().ToString("N")[..11],
        MobileNumber = "010" + Guid.NewGuid().ToString("N")[..8],
        Email = $"{firstName.ToLowerInvariant()}@filters.test.local",
        GovernorateId = governorateId,
        AreaId = areaId,
        IsBlackListed = blacklisted,
        CreatedOn = now.AddDays(-5)
    };

    private static BookCopy CreateBookCopy(Guid bookId, int serialNumber) => new()
    {
        Id = Guid.NewGuid(),
        BookId = bookId,
        EditionNumber = 1,
        SerialNumber = serialNumber,
        IsAvailableForRental = false
    };
}
