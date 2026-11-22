namespace Infrastructure.ViewModel
{
    /// <summary>Scalar option used by report filter dropdowns.</summary>
    public sealed class ReportFilterOption
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>Flattened row of the overdue-books report.</summary>
    public sealed class OverdueBookReport
    {
        public string? SubscriberName { get; set; }
        public string? BookName { get; set; }
        public DateTime RentalDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool PenaltyPaid { get; set; }
    }

    /// <summary>Row of the popular books report.</summary>
    public sealed class PopularBookReport
    {
        public Guid BookId { get; set; }
        public string BookName { get; set; } = string.Empty;
        public string? AuthorName { get; set; }
        public string? CategoryName { get; set; }
        public int RentalCount { get; set; }
    }

    /// <summary>Flattened row of the active-subscribers report.</summary>
    public sealed class ActiveSubscriberReport
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AreaName { get; set; }
        public string? GovernorateName { get; set; }
        public DateTime? ActiveSubscriptionEndDate { get; set; }
    }

    /// <summary>Flattened row of the expiring-subscriptions report.</summary>
    public sealed class ExpiringSubscriptionReport
    {
        public string? SubscriberName { get; set; }
        public string? NationalId { get; set; }
        public string? MobileNumber { get; set; }
        public string? Email { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    /// <summary>Flattened row of the rental-history report (one row per rented copy).</summary>
    public sealed class RentalHistoryReportRow
    {
        public Guid RentalId { get; set; }
        public string? SubscriberName { get; set; }
        public string? BookName { get; set; }
        public DateTime RentalDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? ReturnDate { get; set; }
    }

    /// <summary>Row of the books-by-category report.</summary>
    public sealed class BooksByCategoryReport
    {
        public string? CategoryName { get; set; }
        public int Count { get; set; }
    }

    /// <summary>Row of the books-by-author report.</summary>
    public sealed class BooksByAuthorReport
    {
        public string? AuthorName { get; set; }
        public int Count { get; set; }
    }

    /// <summary>Filter selections and scalar option lists for the book inventory report.</summary>
    public sealed class BookReportFilterViewModel
    {
        public Guid? AuthorId { get; set; }
        public Guid? CategoryId { get; set; }
        public bool AvailableOnly { get; set; }
        public List<ReportFilterOption> Authors { get; set; } = new();
        public List<ReportFilterOption> Categories { get; set; } = new();
    }

    /// <summary>Raw book-inventory filter values supplied by the request.</summary>
    public sealed class BookInventoryQuery
    {
        public Guid? AuthorId { get; set; }
        public Guid? CategoryId { get; set; }
        public bool AvailableOnly { get; set; }
    }

    /// <summary>Flattened row of the full book inventory report.</summary>
    public sealed class BookInventoryReport
    {
        public Guid BookId { get; set; }
        public string BookName { get; set; } = string.Empty;
        public string? ImageThumbnailUrl { get; set; }
        public string? ImageUrl { get; set; }
        public string? AuthorName { get; set; }
        public string? CategoryName { get; set; }
        public string? SubCategoryName { get; set; }
        public string? Publisher { get; set; }
        public string? Hall { get; set; }
        public bool IsAvailableForRental { get; set; }
        public int CopyCount { get; set; }
        public int AvailableCopyCount { get; set; }
        public int RentalCount { get; set; }
    }

    /// <summary>Full book inventory report result, including its filter options.</summary>
    public sealed class BookInventoryReportData
    {
        public List<BookInventoryReport> Rows { get; set; } = new();
        public BookReportFilterViewModel Filter { get; set; } = new();
    }

    /// <summary>Rental history report result, including the subscriber filter options.</summary>
    public sealed class RentalHistoryData
    {
        public List<RentalHistoryReportRow> Rentals { get; set; } = new();
        public List<ReportFilterOption> Subscribers { get; set; } = new();
        public Guid? SelectedSubscriberId { get; set; }
    }

    /// <summary>Expiring-subscriptions report result, including the selected warning window.</summary>
    public sealed class ExpiringSubscriptionsData
    {
        public List<ExpiringSubscriptionReport> Subscriptions { get; set; } = new();
        public int Days { get; set; }
    }
}
