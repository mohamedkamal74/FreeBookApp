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
}
