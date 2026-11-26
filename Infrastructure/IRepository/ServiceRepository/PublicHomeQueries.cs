using Infrastructure.Data;
using Infrastructure.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IRepository.ServiceRepository
{
    /// <summary>
    /// Read-only EF projection for the public home page. It returns scalar card DTOs,
    /// never tracked Domin entities, and leaves the dashboard/report queries untouched.
    /// </summary>
    internal static class PublicHomeQueries
    {
        internal static IQueryable<AvailableBookCardDto> AvailableBooks(FreeBookDbContext context) =>
            from book in context.books.AsNoTracking()
            where book.CurrentState > 0 && book.IsAvailableForRental
            orderby book.Name
            select new AvailableBookCardDto
            {
                BookId = book.Id,
                Title = book.Name,
                AuthorName = book.Author != null ? book.Author.Name : null,
                CategoryName = book.Category != null ? book.Category.Name : null,
                ImageThumbnailUrl = book.ImageThumbnailUrl,
                ImageUrl = book.ImageUrl,
                IsAvailableForRental = book.IsAvailableForRental
            };
    }
}
