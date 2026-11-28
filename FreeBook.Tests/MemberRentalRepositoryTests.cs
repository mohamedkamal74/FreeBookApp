using System;
using System.Linq;
using System.Threading.Tasks;
using Domin.Entity;
using Infrastructure.Data;
using Infrastructure.IRepository.ServiceRepository;
using Infrastructure.ViewModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace FreeBook.Tests;

public class MemberRentalRepositoryTests
{
    [Fact]
    public async Task GetByApplicationUserIdAsync_ReturnsOnlyRentalsOwnedByUser()
    {
        using var context = CreateContext();
        var first = await SeedAsync(context);
        var second = await SeedAsync(context);
        var repository = CreateRepository(context);

        var result = await repository.GetByApplicationUserIdAsync(first.UserId);

        Assert.True(result.IsSuccess, result.Error?.Message);
        var rental = Assert.Single(result.Value!);
        Assert.Equal(first.RentalId, rental.Id);
        Assert.Equal(first.BookTitle, rental.RentalCopies.Single().BookCopy?.Book?.Name);
        Assert.DoesNotContain(second.RentalId, result.Value!.Select(x => x.Id));
    }

    [Fact]
    public async Task ReturnCopyForUserAsync_RequiresOwnerAndMarksCopyAvailable()
    {
        using var context = CreateContext();
        var seed = await SeedAsync(context);
        var repository = CreateRepository(context);

        var wrongOwner = await repository.ReturnCopyForUserAsync(seed.RentalId, seed.CopyId, $"other-{seed.UserId}");
        Assert.False(wrongOwner.IsSuccess);
        Assert.Equal("NOT_FOUND", wrongOwner.Error?.Code);

        var returned = await repository.ReturnCopyForUserAsync(seed.RentalId, seed.CopyId, seed.UserId);
        Assert.True(returned.IsSuccess, returned.Error?.Message);

        var rentalCopy = await context.RentalCopies.SingleAsync(x => x.BookCopyId == seed.CopyId);
        Assert.NotNull(rentalCopy.ReturnDate);
        Assert.True((await context.BookCopies.SingleAsync(x => x.Id == seed.CopyId)).IsAvailableForRental);

        var again = await repository.ReturnCopyForUserAsync(seed.RentalId, seed.CopyId, seed.UserId);
        Assert.False(again.IsSuccess);
        Assert.Equal("ALREADY_RETURNED", again.Error?.Code);
    }

    [Fact]
    public async Task ExtendCopyForUserAsync_CapsAtMaximumAndRejectsReturnedCopies()
    {
        using var context = CreateContext();
        var seed = await SeedAsync(context);
        var repository = CreateRepository(context);

        var extended = await repository.ExtendCopyForUserAsync(
            seed.RentalId,
            seed.CopyId,
            seed.UserId,
            seed.RentalDate.AddDays(30));

        Assert.True(extended.IsSuccess, extended.Error?.Message);
        var rentalCopy = await context.RentalCopies.SingleAsync(x => x.BookCopyId == seed.CopyId);
        Assert.Equal(seed.RentalDate.AddDays((int)RentalsConfigurations.MaxRentalDuration), rentalCopy.EndDate);
        Assert.NotNull(rentalCopy.ExtendedOn);

        Assert.True((await repository.ReturnCopyForUserAsync(seed.RentalId, seed.CopyId, seed.UserId)).IsSuccess);
        var afterReturn = await repository.ExtendCopyForUserAsync(seed.RentalId, seed.CopyId, seed.UserId);
        Assert.False(afterReturn.IsSuccess);
        Assert.Equal("ALREADY_RETURNED", afterReturn.Error?.Code);
    }

    private static ServicesRental CreateRepository(FreeBookDbContext context) =>
        new(context, Mock.Of<ILogger<ServicesRental>>());

    private static FreeBookDbContext CreateContext() => new(new DbContextOptionsBuilder<FreeBookDbContext>()
        .UseInMemoryDatabase($"MemberRentalTests_{Guid.NewGuid():N}")
        .Options);

    private static async Task<SeedData> SeedAsync(FreeBookDbContext context)
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var userId = $"member-rental-{suffix}";
        var now = DateTime.UtcNow;
        var rentalDate = now.AddDays(-10);
        var governorate = new Governorate { Id = Guid.NewGuid(), Name = $"Member Gov {suffix}" };
        var area = new Area
        {
            Id = Guid.NewGuid(),
            Name = $"Member Area {suffix}",
            GovernorateId = governorate.Id
        };
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
            FirstName = "Member",
            LastName = suffix,
            DateOfBirth = now.AddYears(-30),
            NationalId = $"45{suffix}",
            MobileNumber = $"012{suffix}",
            Email = $"subscriber-{suffix}@test.local",
            GovernorateId = governorate.Id,
            AreaId = area.Id,
            ApplicationUserId = userId
        };
        var rentalsBook = new Book
        {
            Id = Guid.NewGuid(),
            Name = $"Member Book {suffix}",
            AuthorId = Guid.NewGuid(),
            CurrentState = (int)Helper.ECurrentState.Active,
            IsAvailableForRental = true
        };
        var copy = new BookCopy
        {
            Id = Guid.NewGuid(),
            BookId = rentalsBook.Id,
            EditionNumber = 1,
            SerialNumber = 1,
            IsAvailableForRental = false
        };
        var rental = new Rental
        {
            Id = Guid.NewGuid(),
            SubscriberId = subscriber.Id,
            StartDate = rentalDate,
            RentalCopies = new System.Collections.Generic.List<RentalCopy>
            {
                new()
                {
                    BookCopyId = copy.Id,
                    RentalDate = rentalDate,
                    EndDate = rentalDate.AddDays((int)RentalsConfigurations.RentalDuration)
                }
            }
        };

        context.AddRange(governorate, area, user, subscriber, rentalsBook, copy, rental);
        await context.SaveChangesAsync();

        return new SeedData(userId, rental.Id, copy.Id, rentalDate, rentalsBook.Name);
    }

    private sealed record SeedData(string UserId, Guid RentalId, Guid CopyId, DateTime RentalDate, string BookTitle);
}
