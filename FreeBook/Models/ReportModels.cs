using Domin.Entity;

namespace FreeBook.Models
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
}
