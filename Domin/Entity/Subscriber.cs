using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domin.Entity
{
    public class Subscriber
    {
        public Guid Id { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources.ResourceData), ErrorMessageResourceName = "FirstName")]
        [MaxLength(100, ErrorMessageResourceType = typeof(Resources.ResourceData), ErrorMessageResourceName = "MaxLength")]
        [MinLength(2, ErrorMessageResourceType = typeof(Resources.ResourceData), ErrorMessageResourceName = "MinLength")]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessageResourceType = typeof(Resources.ResourceData), ErrorMessageResourceName = "LastName")]
        [MaxLength(100, ErrorMessageResourceType = typeof(Resources.ResourceData), ErrorMessageResourceName = "MaxLength")]
        [MinLength(2, ErrorMessageResourceType = typeof(Resources.ResourceData), ErrorMessageResourceName = "MinLength")]
        public string LastName { get; set; } = null!;

        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources.ResourceData), ErrorMessageResourceName = "NationalId")]
        [MaxLength(20, ErrorMessageResourceType = typeof(Resources.ResourceData), ErrorMessageResourceName = "MaxLength")]
        [MinLength(10, ErrorMessageResourceType = typeof(Resources.ResourceData), ErrorMessageResourceName = "MinLength")]
        public string NationalId { get; set; } = null!;

        [Required(ErrorMessageResourceType = typeof(Resources.ResourceData), ErrorMessageResourceName = "MobileNumber")]
        [MaxLength(20, ErrorMessageResourceType = typeof(Resources.ResourceData), ErrorMessageResourceName = "MaxLength")]
        [MinLength(10, ErrorMessageResourceType = typeof(Resources.ResourceData), ErrorMessageResourceName = "MinLength")]
        public string MobileNumber { get; set; } = null!;

        public bool HasWhatsApp { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources.ResourceData), ErrorMessageResourceName = "Email")]
        [EmailAddress(ErrorMessageResourceType = typeof(Resources.ResourceData), ErrorMessageResourceName = "RegisterEmailError")]
        [MaxLength(100, ErrorMessageResourceType = typeof(Resources.ResourceData), ErrorMessageResourceName = "MaxLength")]
        public string Email { get; set; } = null!;

        public string? ImageUrl { get; set; }

        public string? ImageThumbnailUrl { get; set; }

        public Guid AreaId { get; set; }
        public Area? Area { get; set; }

        public Guid GovernorateId { get; set; }
        public Governorate? Governorate { get; set; }

        [MaxLength(500, ErrorMessageResourceType = typeof(Resources.ResourceData), ErrorMessageResourceName = "MaxLength")]
        public string? Address { get; set; }

        public bool IsBlackListed { get; set; } = false;

        public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();

        public ICollection<Rental> Rentals { get; set; } = new List<Rental>();

        public string FullName => $"{FirstName} {LastName}";
    }
}
