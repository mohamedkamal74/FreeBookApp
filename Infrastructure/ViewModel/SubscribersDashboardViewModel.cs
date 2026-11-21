using Domin.Entity;

namespace Infrastructure.ViewModel
{
    /// <summary>Current subscribers-dashboard filter selections plus the governorate options.</summary>
    public class SubscribersDashboardFilterViewModel
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public Guid? GovernorateId { get; set; }
        public List<Governorate> Governorates { get; set; } = new();
    }

    /// <summary>Raw subscribers-dashboard filter values supplied by the request.</summary>
    public class SubscribersDashboardQuery
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public Guid? GovernorateId { get; set; }
    }

    /// <summary>Everything the subscribers dashboard renders, computed once per request.</summary>
    public class SubscribersDashboardViewModel
    {
        public int TotalSubscribers { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int ExpiredSubscriptions { get; set; }
        public int ExpiringSoon { get; set; }
        public int Blacklisted { get; set; }
        public int WhatsAppEnabled { get; set; }

        public List<NamedCount> ByGovernorate { get; set; } = new();
        public List<NamedCount> AgeGroups { get; set; } = new();
        public List<NamedCount> SubscriptionsPerMonth { get; set; } = new();
        public List<NamedCount> SubscriptionStatus { get; set; } = new();
        public List<NamedCount> TopRenters { get; set; } = new();
    }
}
