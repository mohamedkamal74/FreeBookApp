using System;
using System.Collections.Generic;
using System.Linq;
using Domin.Entity;
using Infrastructure.ViewModel;

namespace Infrastructure.Common
{
    public enum RentalRuleError
    {
        None = 0,
        SubscriberNotFound,
        SubscriberBlacklisted,
        NoActiveSubscription,
        NoCopiesSelected,
        TooManyCopies,
        CopyUnavailable,
        DurationExceeded
    }

    public sealed record RentalRuleResult(RentalRuleError Error, RentalCommand? Command)
    {
        public bool IsSuccess => Error == RentalRuleError.None && Command is not null;
    }

    /// <summary>
    /// Rental invariants shared by Admin rentals and member rental requests.
    /// Callers translate <see cref="RentalRuleError"/> to localized messages.
    /// </summary>
    public static class RentalRules
    {
        public static DateTime CapEndDate(DateTime rentalDate, DateTime requestedEndDate)
        {
            var maximum = rentalDate.AddDays((int)RentalsConfigurations.MaxRentalDuration);
            return requestedEndDate > maximum ? maximum : requestedEndDate;
        }

        public static RentalRuleError ValidateSubscriber(SubscriberDto? subscriber, DateTime? nowUtc = null)
        {
            if (subscriber is null)
                return RentalRuleError.SubscriberNotFound;
            if (subscriber.IsBlackListed)
                return RentalRuleError.SubscriberBlacklisted;

            var now = (nowUtc ?? DateTime.UtcNow).ToUniversalTime();
            var hasActiveSubscription = subscriber.Subscriptions?
                .Any(s => s.StartDate <= now && s.EndDate >= now) == true;
            return hasActiveSubscription
                ? RentalRuleError.None
                : RentalRuleError.NoActiveSubscription;
        }

        public static RentalRuleResult BuildCommand(
            SubscriberDto? subscriber,
            IReadOnlyCollection<Guid> selectedCopyIds,
            IReadOnlyCollection<BookCopyDto> allCopies,
            DateTime? nowUtc = null)
        {
            var subscriberError = ValidateSubscriber(subscriber, nowUtc);
            if (subscriberError != RentalRuleError.None)
                return new RentalRuleResult(subscriberError, null);

            var copies = (selectedCopyIds ?? Array.Empty<Guid>()).Distinct().ToArray();
            if (copies.Length == 0)
                return new RentalRuleResult(RentalRuleError.NoCopiesSelected, null);
            if (copies.Length > (int)RentalsConfigurations.MaxAllowedCopies)
                return new RentalRuleResult(RentalRuleError.TooManyCopies, null);

            var available = (allCopies ?? Array.Empty<BookCopyDto>())
                .Where(c => copies.Contains(c.Id) && c.IsAvailableForRental)
                .ToList();
            if (available.Count != copies.Length)
                return new RentalRuleResult(RentalRuleError.CopyUnavailable, null);

            var now = (nowUtc ?? DateTime.UtcNow).ToUniversalTime();
            var endDate = CapEndDate(now, now.AddDays((int)RentalsConfigurations.RentalDuration));
            if (endDate > now.AddDays((int)RentalsConfigurations.MaxRentalDuration))
                return new RentalRuleResult(RentalRuleError.DurationExceeded, null);

            var command = new RentalCommand
            {
                SubscriberId = subscriber!.Id,
                StartDate = now,
                PenaltyPaid = false,
                RentalCopies = available.Select(copy => new RentalCopyCommand
                {
                    BookCopyId = copy.Id,
                    RentalDate = now,
                    EndDate = endDate
                }).ToList()
            };
            return new RentalRuleResult(RentalRuleError.None, command);
        }
    }
}
