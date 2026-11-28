using System;
using System.Collections.Generic;
using Domin.Entity;
using Infrastructure.Common;
using Infrastructure.ViewModel;

namespace FreeBook.Tests;

public class RentalRulesTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void BuildCommand_UsesSevenDayRentalForActiveSubscriber()
    {
        var subscriber = CreateSubscriber(active: true, blacklisted: false);
        var first = new BookCopyDto { Id = Guid.NewGuid(), IsAvailableForRental = true };
        var second = new BookCopyDto { Id = Guid.NewGuid(), IsAvailableForRental = true };

        var result = RentalRules.BuildCommand(
            subscriber,
            new[] { first.Id, second.Id },
            new[] { first, second },
            Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(subscriber.Id, result.Command!.SubscriberId);
        Assert.Equal(Now, result.Command.StartDate);
        Assert.Equal(2, result.Command.RentalCopies.Count);
        Assert.All(result.Command.RentalCopies, copy =>
        {
            Assert.Equal(Now, copy.RentalDate);
            Assert.Equal(Now.AddDays((int)RentalsConfigurations.RentalDuration), copy.EndDate);
        });
    }

    [Fact]
    public void BuildCommand_RejectsUnavailableSubscriberAndCopies()
    {
        var active = CreateSubscriber(active: true, blacklisted: false);
        var blacklisted = CreateSubscriber(active: true, blacklisted: true);
        var expired = CreateSubscriber(active: false, blacklisted: false);
        var available = new BookCopyDto { Id = Guid.NewGuid(), IsAvailableForRental = true };
        var rented = new BookCopyDto { Id = Guid.NewGuid(), IsAvailableForRental = false };
        var copies = new[] { available, rented };

        Assert.Equal(RentalRuleError.SubscriberNotFound,
            RentalRules.BuildCommand(null, new[] { available.Id }, copies, Now).Error);
        Assert.Equal(RentalRuleError.SubscriberBlacklisted,
            RentalRules.BuildCommand(blacklisted, new[] { available.Id }, copies, Now).Error);
        Assert.Equal(RentalRuleError.NoActiveSubscription,
            RentalRules.BuildCommand(expired, new[] { available.Id }, copies, Now).Error);
        Assert.Equal(RentalRuleError.NoCopiesSelected,
            RentalRules.BuildCommand(active, Array.Empty<Guid>(), copies, Now).Error);
        Assert.Equal(RentalRuleError.CopyUnavailable,
            RentalRules.BuildCommand(active, new[] { rented.Id }, copies, Now).Error);

        var tooMany = new List<Guid>();
        for (var index = 0; index <= (int)RentalsConfigurations.MaxAllowedCopies; index++)
        {
            tooMany.Add(Guid.NewGuid());
            copies = new[] { available, rented };
        }
        Assert.Equal(RentalRuleError.TooManyCopies,
            RentalRules.BuildCommand(active, tooMany, copies, Now).Error);
    }

    [Fact]
    public void CapEndDate_LimitsRentalAtMaximumDuration()
    {
        var requested = Now.AddDays(30);
        var capped = RentalRules.CapEndDate(Now, requested);

        Assert.Equal(Now.AddDays((int)RentalsConfigurations.MaxRentalDuration), capped);
        Assert.Equal(Now.AddDays(3), RentalRules.CapEndDate(Now, Now.AddDays(3)));
    }

    private static SubscriberDto CreateSubscriber(bool active, bool blacklisted)
    {
        var now = Now;
        return new SubscriberDto
        {
            Id = Guid.NewGuid(),
            FirstName = "Rule",
            LastName = "Subscriber",
            IsBlackListed = blacklisted,
            Subscriptions = new List<SubscriptionDto>
            {
                new()
                {
                    StartDate = now.AddDays(-1),
                    EndDate = active ? now.AddDays(1) : now.AddDays(-1)
                }
            }
        };
    }
}
