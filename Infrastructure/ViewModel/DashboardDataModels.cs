namespace Infrastructure.ViewModel
{
    /// <summary>Flattened recent-rental row shown on the general dashboard.</summary>
    public sealed class RecentRentalRow
    {
        public string? SubscriberName { get; set; }
        public DateTime StartDate { get; set; }
        public int RentalCopyCount { get; set; }
    }

    /// <summary>Flattened overdue-rental row shown on the general dashboard.</summary>
    public sealed class OverdueRentalRow
    {
        public string? SubscriberName { get; set; }
        public string? BookName { get; set; }
        public DateTime EndDate { get; set; }
        public bool PenaltyPaid { get; set; }
    }

    /// <summary>Flattened expiring-subscription row shown on the general dashboard.</summary>
    public sealed class ExpiringSubscriptionRow
    {
        public string? SubscriberName { get; set; }
        public DateTime EndDate { get; set; }
    }

    /// <summary>Flattened recent-subscriber row shown on the general dashboard.</summary>
    public sealed class RecentSubscriberRow
    {
        public string? FullName { get; set; }
        public string? AreaName { get; set; }
        public string? GovernorateName { get; set; }
    }

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

        public List<RecentRentalRow> RecentRentals { get; set; } = new();
        public List<RecentSubscriberRow> RecentSubscribers { get; set; } = new();
        public List<OverdueRentalRow> OverdueList { get; set; } = new();
        public List<ExpiringSubscriptionRow> ExpiringSubs { get; set; } = new();

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
