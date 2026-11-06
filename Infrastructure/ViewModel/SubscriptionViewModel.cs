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
    public class SubscriptionViewModel
    {
        public List<Subscription> Subscriptions { get; set; } = new();
        public List<Subscriber> Subscribers { get; set; } = new();
        public List<Infrastructure.ViewModel.ApplicationUser> Admins { get; set; } = new();
        public NewSubscription NewSubscription { get; set; } = new();
    }

    public class NewSubscription
    {
        public string? SubscriptionId { get; set; }

        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "SubscriberId")]
        public Guid SubscriberId { get; set; }

        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "StartDate")]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [Required(ErrorMessageResourceType = typeof(ResourceData), ErrorMessageResourceName = "EndDate")]
        public DateTime EndDate { get; set; } = DateTime.Now.AddYears(1);

        public string? CreatedById { get; set; }
    }
}
