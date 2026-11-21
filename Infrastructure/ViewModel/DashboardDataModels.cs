using Domin.Entity;

namespace Infrastructure.ViewModel
{
    /// <summary>Everything the general dashboard renders, computed once per request.</summary>
    public class GeneralDashboardData
    {
        public int TotalBooks { get; set; }
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
        public int TotalSubscribers { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int ActiveRentals { get; set; }
        public int OverdueRentals { get; set; }
        public int ExpiringSubscriptions { get; set; }
        public int BlacklistedSubscribers { get; set; }

        public List<Rental> RecentRentals { get; set; } = new();
        public List<Subscriber> RecentSubscribers { get; set; } = new();
        public List<Rental> OverdueList { get; set; } = new();
        public List<Subscription> ExpiringSubs { get; set; } = new();

        public DashboardFilterViewModel Filter { get; set; } = new();
        public DashboardChartsViewModel Charts { get; set; } = new();
    }

    /// <summary>Everything the subscribers dashboard renders, computed once per request.</summary>
    public class SubscribersDashboardData
    {
        public SubscribersDashboardViewModel Dashboard { get; set; } = new();
        public SubscribersDashboardFilterViewModel Filter { get; set; } = new();
    }

    /// <summary>Everything the books dashboard renders, computed once per request.</summary>
    public class BooksDashboardData
    {
        public BooksDashboardViewModel Dashboard { get; set; } = new();
        public BooksDashboardFilterViewModel Filter { get; set; } = new();
    }
}
