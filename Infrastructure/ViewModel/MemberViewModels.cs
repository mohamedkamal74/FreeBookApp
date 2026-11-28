using System;
using System.Collections.Generic;

namespace Infrastructure.ViewModel
{
    /// <summary>Read model for the self-service ordering page.</summary>
    public sealed class MemberOrdersViewModel
    {
        public List<AvailableBookCardDto> AvailableBooks { get; set; } = new();
        public List<RentalRequestDto> Requests { get; set; } = new();
    }

    /// <summary>Read model for the member's rentals and their copies.</summary>
    public sealed class MemberRentalsViewModel
    {
        public List<RentalDto> Rentals { get; set; } = new();
    }

    public sealed class MemberOrderCommand
    {
        public Guid BookId { get; set; }
        public Guid? BookCopyId { get; set; }
    }

    public sealed class MemberRequestAction
    {
        public Guid RequestId { get; set; }
    }

    /// <summary>
    /// Targets one rented copy. NewEndDate is optional; an empty value extends by
    /// the standard seven-day window and the repository caps it at the maximum.
    /// </summary>
    public sealed class MemberRentalAction
    {
        public Guid RentalId { get; set; }
        public Guid BookCopyId { get; set; }
        public string? NewEndDate { get; set; }
    }
}
