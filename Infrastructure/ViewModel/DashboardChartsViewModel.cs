namespace Infrastructure.ViewModel
{
    /// <summary>A display label paired with a count, used to feed the dashboard charts.</summary>
    public class NamedCount
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    /// <summary>Everything the general dashboard charts need, computed once per request.</summary>
    public class DashboardChartsViewModel
    {
        public List<NamedCount> TopRentedBooks { get; set; } = new();
        public List<NamedCount> TopRenters { get; set; } = new();
        public List<NamedCount> BooksByCategory { get; set; } = new();
        public List<NamedCount> RentalsPerMonth { get; set; } = new();
        public int AvailableCopies { get; set; }
        public int RentedCopies { get; set; }
    }
}
