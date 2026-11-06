using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domin.Entity;
using Domin.Resources;
using Infrastructure.ViewModel;

namespace Infrastructure.ViewModel
{
    public class SubscriberViewModel
    {
        public List<Subscriber> Subscribers { get; set; } = new();
        public List<Area> Areas { get; set; } = new();
        public List<Governorate> Governorates { get; set; } = new();
        public NewSubscriber NewSubscriber { get; set; } = new();
    }

    public class NewSubscriber
    {
        public string? SubscriberId { get; set; }

        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "FirstName")]
        [MaxLength(100, ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "MaxLength")]
        [MinLength(2, ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "MinLength")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "LastName")]
        [MaxLength(100, ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "MaxLength")]
        [MinLength(2, ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "MinLength")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "NationalId")]
        [MaxLength(20, ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "MaxLength")]
        [MinLength(10, ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "MinLength")]
        public string NationalId { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "MobileNumber")]
        [MaxLength(20, ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "MaxLength")]
        [MinLength(10, ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "MinLength")]
        public string MobileNumber { get; set; } = string.Empty;

        public bool HasWhatsApp { get; set; }

        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "Email")]
        [EmailAddress(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "RegisterEmailError")]
        [MaxLength(100, ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "MaxLength")]
        public string Email { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }
        public string? ImageThumbnailUrl { get; set; }

        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "AreaName")]
        public Guid AreaId { get; set; }

        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "GovernorateName")]
        public Guid GovernorateId { get; set; }

        [MaxLength(500, ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "MaxLength")]
        public string? Address { get; set; }

        public bool IsBlackListed { get; set; }

        public DateTime DateOfBirth { get; set; } = DateTime.Now.AddYears(-18);
    }
}
