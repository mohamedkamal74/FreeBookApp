using Domin.Entity;

namespace Infrastructure.ViewModel
{
    /// <summary>Row of the popular books report (book + how many times it was rented).</summary>
    public class PopularBookReport
    {
        public Book Book { get; set; } = null!;
        public int RentalCount { get; set; }
    }

    /// <summary>Row of the books-by-category report.</summary>
    public class BooksByCategoryReport
    {
        public Category? Category { get; set; }
        public int Count { get; set; }
    }

    /// <summary>Row of the books-by-author report.</summary>
    public class BooksByAuthorReport
    {
        public Author? Author { get; set; }
        public int Count { get; set; }
    }

    /// <summary>Filter selections and option lists for the book inventory report.</summary>
    public class BookReportFilterViewModel
    {
        public Guid? AuthorId { get; set; }
        public Guid? CategoryId { get; set; }
        public bool AvailableOnly { get; set; }
        public List<Author> Authors { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
    }

    /// <summary>Raw book-inventory filter values supplied by the request.</summary>
    public class BookInventoryQuery
    {
        public Guid? AuthorId { get; set; }
        public Guid? CategoryId { get; set; }
        public bool AvailableOnly { get; set; }
    }

    /// <summary>One row of the full book inventory report.</summary>
    public class BookInventoryReport
    {
        public Book Book { get; set; } = null!;
        public string? AuthorName { get; set; }
        public string? CategoryName { get; set; }
        public string? SubCategoryName { get; set; }
        public int CopyCount { get; set; }
        public int AvailableCopyCount { get; set; }
        public int RentalCount { get; set; }
    }

    /// <summary>Full book inventory report result, including its filter options.</summary>
    public class BookInventoryReportData
    {
        public List<BookInventoryReport> Rows { get; set; } = new();
        public BookReportFilterViewModel Filter { get; set; } = new();
    }

    /// <summary>Rental history report result, including the subscriber filter options.</summary>
    public class RentalHistoryData
    {
        public List<Rental> Rentals { get; set; } = new();
        public List<Subscriber> Subscribers { get; set; } = new();
        public Guid? SelectedSubscriberId { get; set; }
    }

    /// <summary>Expiring-subscriptions report result, including the selected warning window.</summary>
    public class ExpiringSubscriptionsData
    {
        public List<Subscription> Subscriptions { get; set; } = new();
        public int Days { get; set; }
    }
}
