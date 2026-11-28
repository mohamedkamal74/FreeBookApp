using System;
using System.Linq;
using System.Threading.Tasks;
using Domin.Entity;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.IRepository.ServiceRepository;
using Infrastructure.ViewModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace FreeBook.Tests;

public class RentalRequestRepositoryTests
{
    [Fact]
    public async Task CreateForUserAsync_RequiresLinkAndActiveSubscription_AndBlocksDuplicate()
    {
        using var context = CreateContext();
        var seed = await SeedAsync(context);
        var requests = new ServicesRentalRequest(context, Mock.Of<ILogger<ServicesRentalRequest>>());

        var created = await requests.CreateForUserAsync(seed.UserId, seed.BookId, seed.CopyId);
        Assert.True(created.IsSuccess, created.Error?.Message);
        var dto = (await requests.FindByIdAsync(created.Value)).Value;
        Assert.NotNull(dto);
        Assert.Equal(seed.SubscriberId, dto!.SubscriberId);
        Assert.Equal(seed.UserId, dto.ApplicationUserId);
        Assert.Equal(RentalRequestStatusDto.Pending, dto.Status);
        Assert.Equal("Request Book", dto.BookTitle);

        var duplicate = await requests.CreateForUserAsync(seed.UserId, seed.BookId);
        Assert.False(duplicate.IsSuccess);
        Assert.Equal("DUPLICATE_PENDING", duplicate.Error?.Code);

        var unlinked = await requests.CreateForUserAsync($"unlinked-{Guid.NewGuid():N}", seed.BookId);
        Assert.False(unlinked.IsSuccess);
        Assert.Equal("SUBSCRIBER_NOT_LINKED", unlinked.Error?.Code);
    }

    [Fact]
    public async Task CreateForUserAsync_RejectsSubscriberWithoutActiveSubscription()
    {
        using var context = CreateContext();
        var seed = await SeedAsync(context, active: false);
        var requests = new ServicesRentalRequest(context, Mock.Of<ILogger<ServicesRentalRequest>>());

        var result = await requests.CreateForUserAsync(seed.UserId, seed.BookId);

        Assert.False(result.IsSuccess);
        Assert.Equal("RULE_NoActiveSubscription", result.Error?.Code);
        Assert.Empty(await context.RentalRequests.ToListAsync());
    }

    [Fact]
    public async Task ApproveAsync_CreatesRentalAndMarksCopyUnavailable()
    {
        using var context = CreateContext();
        var seed = await SeedAsync(context);
        var requests = new ServicesRentalRequest(context, Mock.Of<ILogger<ServicesRentalRequest>>());
        var requestId = (await requests.CreateForUserAsync(seed.UserId, seed.BookId, seed.CopyId)).Value;
        var before = DateTime.UtcNow;

        var approved = await requests.ApproveAsync(requestId, new[] { seed.CopyId }, "admin-id");

        Assert.True(approved.IsSuccess, approved.Error?.Message);
        var rental = await context.Rentals.Include(x => x.RentalCopies).SingleAsync();
        Assert.Equal(seed.SubscriberId, rental.SubscriberId);
        var rentalCopy = Assert.Single(rental.RentalCopies);
        Assert.Equal(seed.CopyId, rentalCopy.BookCopyId);
        Assert.InRange(rentalCopy.EndDate, before.AddDays(7), DateTime.UtcNow.AddDays(7));

        var copy = await context.BookCopies.SingleAsync(x => x.Id == seed.CopyId);
        Assert.False(copy.IsAvailableForRental);
        var request = await context.RentalRequests.SingleAsync();
        Assert.Equal(RentalRequestStatus.Approved, request.Status);
        Assert.Equal("admin-id", request.ReviewedById);
        Assert.NotNull(request.ReviewedOn);
    }

    [Fact]
    public async Task ApproveAsync_RejectsUnavailableCopyWithoutMutatingRequest()
    {
        using var context = CreateContext();
        var seed = await SeedAsync(context);
        var requests = new ServicesRentalRequest(context, Mock.Of<ILogger<ServicesRentalRequest>>());
        var requestId = (await requests.CreateForUserAsync(seed.UserId, seed.BookId)).Value;

        var copy = await context.BookCopies.SingleAsync(x => x.Id == seed.CopyId);
        copy.IsAvailableForRental = false;
        await context.SaveChangesAsync();

        var approved = await requests.ApproveAsync(requestId, new[] { seed.CopyId }, null);

        Assert.False(approved.IsSuccess);
        Assert.Equal("RULE_CopyUnavailable", approved.Error?.Code);
        Assert.Empty(await context.Rentals.ToListAsync());
        Assert.Equal(RentalRequestStatus.Pending, (await context.RentalRequests.SingleAsync()).Status);
    }

    [Fact]
    public async Task RejectAndCancel_RespectOwnerAndPendingStatus()
    {
        using var context = CreateContext();
        var seed = await SeedAsync(context);
        var requests = new ServicesRentalRequest(context, Mock.Of<ILogger<ServicesRentalRequest>>());
        var requestId = (await requests.CreateForUserAsync(seed.UserId, seed.BookId)).Value;

        var rejected = await requests.RejectAsync(requestId, "admin-id", "Not available");
        Assert.True(rejected.IsSuccess, rejected.Error?.Message);
        Assert.Equal(RentalRequestStatus.Rejected, (await context.RentalRequests.SingleAsync()).Status);

        var cancelRejected = await requests.CancelAsync(requestId, seed.UserId);
        Assert.False(cancelRejected.IsSuccess);
        Assert.Equal("INVALID_STATUS", cancelRejected.Error?.Code);

        var otherSeed = await SeedAsync(context);
        var otherRequestId = (await requests.CreateForUserAsync(otherSeed.UserId, otherSeed.BookId)).Value;
        var wrongOwner = await requests.CancelAsync(otherRequestId, seed.UserId);
        Assert.False(wrongOwner.IsSuccess);
        Assert.Equal("FORBIDDEN", wrongOwner.Error?.Code);

        var cancelled = await requests.CancelAsync(otherRequestId, otherSeed.UserId);
        Assert.True(cancelled.IsSuccess, cancelled.Error?.Message);
        Assert.Equal(RentalRequestStatus.Cancelled,
            (await context.RentalRequests.SingleAsync(x => x.Id == otherRequestId)).Status);
    }

    [Fact]
    public async Task SubscriberSaveAsync_ValidatesUserLinkUniqueness()
    {
        using var context = CreateContext();
        var seed = await SeedAsync(context);
        var subscribers = new ServicesSubscriber(context, Mock.Of<ILogger<ServicesSubscriber>>());

        var unknownUser = new SubscriberCommand
        {
            FirstName = "Unknown",
            LastName = "User",
            NationalId = "10000000001",
            MobileNumber = "01000000001",
            Email = "unknown@test.local",
            AreaId = seed.AreaId,
            GovernorateId = seed.GovernorateId,
            ApplicationUserId = $"missing-{Guid.NewGuid():N}"
        };
        Assert.False((await subscribers.SaveAsync(unknownUser)).IsSuccess);

        var duplicateLink = new SubscriberCommand
        {
            FirstName = "Duplicate",
            LastName = "Link",
            NationalId = "10000000002",
            MobileNumber = "01000000002",
            Email = "duplicate@test.local",
            AreaId = seed.AreaId,
            GovernorateId = seed.GovernorateId,
            ApplicationUserId = seed.UserId
        };
        var result = await subscribers.SaveAsync(duplicateLink);
        Assert.False(result.IsSuccess);
        Assert.Equal("USER_ALREADY_LINKED", result.Error?.Code);
        Assert.Single(await context.Subscribers.ToListAsync());
    }

    [Fact]
    public async Task GetByApplicationUserIdAsync_ReturnsOnlyLinkedUsersRequests()
    {
        using var context = CreateContext();
        var first = await SeedAsync(context);
        var second = await SeedAsync(context);
        var requests = new ServicesRentalRequest(context, Mock.Of<ILogger<ServicesRentalRequest>>());

        var firstRequestId = (await requests.CreateForUserAsync(first.UserId, first.BookId)).Value;
        var secondRequestId = (await requests.CreateForUserAsync(second.UserId, second.BookId)).Value;

        var result = await requests.GetByApplicationUserIdAsync(first.UserId);

        Assert.True(result.IsSuccess, result.Error?.Message);
        var request = Assert.Single(result.Value!);
        Assert.Equal(firstRequestId, request.Id);
        Assert.Equal(first.UserId, request.ApplicationUserId);
        Assert.DoesNotContain(secondRequestId, result.Value!.Select(x => x.Id));
    }

    private static FreeBookDbContext CreateContext() => new(new DbContextOptionsBuilder<FreeBookDbContext>()
        .UseInMemoryDatabase($"RentalRequestTests_{Guid.NewGuid():N}")
        .Options);

    private static async Task<SeedData> SeedAsync(FreeBookDbContext context, bool active = true)
    {
        var now = DateTime.UtcNow;
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var governorate = new Governorate { Id = Guid.NewGuid(), Name = $"Request Gov {suffix}" };
        var area = new Area { Id = Guid.NewGuid(), Name = $"Request Area {suffix}", GovernorateId = governorate.Id };
        var book = new Book
        {
            Id = Guid.NewGuid(),
            Name = "Request Book",
            AuthorId = Guid.NewGuid(),
            IsAvailableForRental = true,
            CurrentState = (int)Helper.ECurrentState.Active
        };
        var firstCopy = new BookCopy
        {
            Id = Guid.NewGuid(),
            BookId = book.Id,
            EditionNumber = 1,
            SerialNumber = 1,
            IsAvailableForRental = true
        };
        var secondCopy = new BookCopy
        {
            Id = Guid.NewGuid(),
            BookId = book.Id,
            EditionNumber = 1,
            SerialNumber = 2,
            IsAvailableForRental = true
        };
        var userId = $"request-user-{suffix}";
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = $"{userId}@test.local",
            Email = $"{userId}@test.local",
            ActiveUser = true,
            EmailConfirmed = true
        };
        var subscriber = new Subscriber
        {
            Id = Guid.NewGuid(),
            FirstName = "Request",
            LastName = "Subscriber",
            DateOfBirth = now.AddYears(-30),
            NationalId = $"99{suffix}",
            MobileNumber = $"011{suffix}",
            Email = $"subscriber-{suffix}@test.local",
            GovernorateId = governorate.Id,
            AreaId = area.Id,
            ApplicationUserId = userId
        };

        context.AddRange(governorate, area, book, firstCopy, secondCopy, user, subscriber);
        context.Add(new Subscription
        {
            Id = Guid.NewGuid(),
            SubscriberId = subscriber.Id,
            StartDate = now.AddDays(-1),
            EndDate = active ? now.AddDays(10) : now.AddDays(-1)
        });
        await context.SaveChangesAsync();

        return new SeedData(userId, subscriber.Id, area.Id, governorate.Id, book.Id, firstCopy.Id, secondCopy.Id);
    }

    private sealed record SeedData(
        string UserId,
        Guid SubscriberId,
        Guid AreaId,
        Guid GovernorateId,
        Guid BookId,
        Guid CopyId,
        Guid OtherCopyId);
}
