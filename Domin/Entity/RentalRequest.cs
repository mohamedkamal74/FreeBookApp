using System;

namespace Domin.Entity
{
    public enum RentalRequestStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3,
        Cancelled = 4
    }

    /// <summary>
    /// A member's request to rent a book. Nothing is checked out until an admin
    /// selects an available copy and approves it.
    /// </summary>
    public class RentalRequest
    {
        public Guid Id { get; set; }

        public Guid SubscriberId { get; set; }
        public Subscriber? Subscriber { get; set; }

        public Guid BookId { get; set; }
        public Book? Book { get; set; }

        /// <summary>Optional copy preferred by the member; the admin may override it.</summary>
        public Guid? BookCopyId { get; set; }
        public BookCopy? BookCopy { get; set; }

        public RentalRequestStatus Status { get; set; } = RentalRequestStatus.Pending;

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedOn { get; set; }
        public string? ReviewedById { get; set; }
        public string? RejectionReason { get; set; }
    }
}
