using System;
using System.ComponentModel.DataAnnotations;

namespace Domin.Entity
{
    /// <summary>
    /// The kind of library event that produced an admin notification. Stored as
    /// an int so the list can be ordered and filtered cheaply.
    /// </summary>
    public enum NotificationType
    {
        SubscriptionExpiring = 1,
        RentalOverdue = 2,
        FineApplied = 3
    }

    /// <summary>
    /// An alert produced by a Hangfire recurring job for the admin notification
    /// center. Messages are localized at render time from <see cref="Type"/> and
    /// the fields below, because background jobs run without a UI culture.
    /// </summary>
    public class Notification
    {
        public Guid Id { get; set; }

        public NotificationType Type { get; set; }

        /// <summary>
        /// Stable identity of the underlying event (for example
        /// "sub:{subscriptionId}:{daysStage}"). Upsert uses it to keep one row
        /// per case even when a recurring job runs every day.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string DedupeKey { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string SubscriberName { get; set; } = null!;

        [MaxLength(300)]
        public string? BookTitle { get; set; }

        /// <summary>Days until expiry, or days overdue, depending on <see cref="Type"/>.</summary>
        public int? Days { get; set; }

        /// <summary>Fine amount, only set for <see cref="NotificationType.FineApplied"/>.</summary>
        public decimal? Amount { get; set; }

        [MaxLength(300)]
        public string? TargetUrl { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; }

        public DateTime? ReadOn { get; set; }
    }
}
