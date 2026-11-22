namespace Infrastructure.ViewModel
{
    /// <summary>Current books-dashboard filter selections plus the option lists for the filter form.</summary>
    public class BooksDashboardFilterViewModel
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public Guid? AuthorId { get; set; }
        public Guid? CategoryId { get; set; }
        public List<ReportFilterOption> Authors { get; set; } = new();
        public List<ReportFilterOption> Categories { get; set; } = new();
    }

    /// <summary>Raw books-dashboard filter values supplied by the request.</summary>
    public class BooksDashboardQuery
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public Guid? AuthorId { get; set; }
        public Guid? CategoryId { get; set; }
    }

    /// <summary>Everything the books dashboard renders, computed once per request.</summary>
    public class BooksDashboardViewModel
    {
        public int TotalBooks { get; set; }
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
        public int RentedCopies { get; set; }
        public int BooksWithoutCopies { get; set; }

        public List<NamedCount> ByCategory { get; set; } = new();
        public List<NamedCount> ByAuthor { get; set; } = new();
        public List<NamedCount> TopRentedBooks { get; set; } = new();
        public List<NamedCount> RentalsPerMonth { get; set; } = new();
        public List<NamedCount> Availability { get; set; } = new();
    }
}
