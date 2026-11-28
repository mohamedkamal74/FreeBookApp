using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domin.Resources;
using Infrastructure.ViewModel;

namespace Infrastructure.ViewModel
{
    public class RentalViewModel
    {
        public List<RentalDto> Rentals { get; set; } = new();
        public List<SubscriberDto> Subscribers { get; set; } = new();
        public List<BookCopyDto> AvailableCopies { get; set; } = new();
        public List<RentalRequestDto> PendingRequests { get; set; } = new();
        public NewRental NewRental { get; set; } = new();
        public Guid[] SelectedCopyIds { get; set; } = Array.Empty<Guid>();

        // For return/extend actions
        public Guid RentalId { get; set; }
        public Guid RentalCopyId { get; set; }
        public DateTime ReturnDate { get; set; } = DateTime.Now;
        public bool HasPenalty { get; set; }
        public DateTime NewEndDate { get; set; }
    }

    public class NewRental
    {
        public string? RentalId { get; set; }

        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "SubscriberId")]
        public Guid SubscriberId { get; set; }

        public DateTime StartDate { get; set; } = DateTime.Now;

        public bool PenaltyPaid { get; set; }

        public List<Guid> BookCopyIds { get; set; } = new();
    }

    /// <summary>Input for Admin approval/rejection of a member rental request.</summary>
    public class RentalRequestViewModel
    {
        public Guid RequestId { get; set; }
        public Guid[] SelectedCopyIds { get; set; } = Array.Empty<Guid>();
        public string? RejectionReason { get; set; }
    }

    public class RentalReturnViewModel
    {
        public Guid RentalId { get; set; }
        public Guid RentalCopyId { get; set; }
        public DateTime ReturnDate { get; set; } = DateTime.Now;
        public bool HasPenalty { get; set; }
    }

    public class RentalExtendViewModel
    {
        public Guid RentalId { get; set; }
        public Guid RentalCopyId { get; set; }
        public DateTime NewEndDate { get; set; }
    }
}
