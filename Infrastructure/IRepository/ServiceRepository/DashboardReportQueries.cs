using Domin.Entity;
using Infrastructure.Data;
using Infrastructure.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IRepository.ServiceRepository
{
    /// <summary>
    /// Reusable read-only EF queries for dashboards and reports. These queries keep the
    /// filters and Includes that used to live in the generic service repositories.
    /// </summary>
    internal static class DashboardReportQueries
    {
        internal static IQueryable<Book> Books(FreeBookDbContext context) =>
            context.books.AsNoTracking();

        internal static IQueryable<Book> ActiveBooks(FreeBookDbContext context) =>
            Books(context).Where(book => book.CurrentState > 0);

        internal static IQueryable<Author> ActiveAuthors(FreeBookDbContext context) =>
            context.Authors.AsNoTracking().Where(author => !author.IsDeleted);

        internal static IQueryable<Category> ActiveCategories(FreeBookDbContext context) =>
            context.categories.AsNoTracking().Where(category => category.Currentstste > 0);

        internal static IQueryable<Rental> Rentals(FreeBookDbContext context) =>
            context.Rentals.AsNoTracking()
                .Include(rental => rental.Subscriber)
                .Include(rental => rental.RentalCopies)
                    .ThenInclude(copy => copy.BookCopy)
                        .ThenInclude(bookCopy => bookCopy.Book);

        internal static IQueryable<Subscriber> Subscribers(FreeBookDbContext context) =>
            context.Subscribers.AsNoTracking()
                .Include(subscriber => subscriber.Area)
                .Include(subscriber => subscriber.Governorate);

        internal static IQueryable<Subscription> Subscriptions(FreeBookDbContext context) =>
            context.Subscriptions.AsNoTracking()
                .Include(subscription => subscription.Subscriber);

        internal static IQueryable<BookCopy> BookCopies(FreeBookDbContext context) =>
            context.BookCopies.AsNoTracking()
                .Include(copy => copy.Book);

        internal static IQueryable<Governorate> Governorates(FreeBookDbContext context) =>
            context.Governorates.AsNoTracking()
                .Include(governorate => governorate.Areas);

        internal static IQueryable<NamedCount> TopRentedBooks(FreeBookDbContext context) =>
            from rentalCopy in context.RentalCopies.AsNoTracking()
            join bookCopy in context.BookCopies.AsNoTracking() on rentalCopy.BookCopyId equals bookCopy.Id
            join book in Books(context) on bookCopy.BookId equals book.Id
            group book by new { book.Id, book.Name } into grouped
            select new NamedCount
            {
                Name = grouped.Key.Name,
                Count = grouped.Count()
            };

        internal static IQueryable<NamedCount> TopRenters(FreeBookDbContext context) =>
            from rental in context.Rentals.AsNoTracking()
            join subscriber in context.Subscribers.AsNoTracking() on rental.SubscriberId equals subscriber.Id
            group subscriber by new { subscriber.Id, subscriber.FirstName, subscriber.LastName } into grouped
            select new NamedCount
            {
                Name = grouped.Key.FirstName + " " + grouped.Key.LastName,
                Count = grouped.Count()
            };

        internal static IQueryable<NamedCount> BooksByCategory(
            FreeBookDbContext context,
            Guid? authorId,
            Guid? categoryId) =>
            from book in ActiveBooks(context)
                .Where(book => !authorId.HasValue || book.AuthorId == authorId.Value)
                .Where(book => !categoryId.HasValue || book.CategoryId == categoryId.Value)
            group book by book.Category != null ? book.Category.Name : "—" into grouped
            select new NamedCount
            {
                Name = grouped.Key,
                Count = grouped.Count()
            };

        internal static IQueryable<NamedCount> BooksByAuthor(
            FreeBookDbContext context,
            Guid? authorId,
            Guid? categoryId) =>
            from book in ActiveBooks(context)
                .Where(book => !authorId.HasValue || book.AuthorId == authorId.Value)
                .Where(book => !categoryId.HasValue || book.CategoryId == categoryId.Value)
            group book by book.Author != null ? book.Author.Name : "—" into grouped
            select new NamedCount
            {
                Name = grouped.Key,
                Count = grouped.Count()
            };

        internal static IQueryable<NamedCount> SubscribersByGovernorate(
            FreeBookDbContext context,
            Guid? governorateId) =>
            from subscriber in context.Subscribers.AsNoTracking()
                .Where(subscriber => !governorateId.HasValue || subscriber.GovernorateId == governorateId.Value)
            group subscriber by subscriber.Governorate != null ? subscriber.Governorate.Name : "—" into grouped
            select new NamedCount
            {
                Name = grouped.Key,
                Count = grouped.Count()
            };

        internal static IQueryable<NamedCount> TopSubscriberRenters(
            FreeBookDbContext context,
            DateTime rangeFrom,
            DateTime toExclusive,
            Guid? governorateId) =>
            from rental in context.Rentals.AsNoTracking()
            join subscriber in context.Subscribers.AsNoTracking() on rental.SubscriberId equals subscriber.Id
            where !governorateId.HasValue || subscriber.GovernorateId == governorateId.Value
            where rental.StartDate >= rangeFrom && rental.StartDate < toExclusive
            group subscriber by new { subscriber.Id, subscriber.FirstName, subscriber.LastName } into grouped
            select new NamedCount
            {
                Name = grouped.Key.FirstName + " " + grouped.Key.LastName,
                Count = grouped.Count()
            };

        internal static IQueryable<ReportQueryHelpers.MonthCount> RentalMonths(
            FreeBookDbContext context,
            DateTime rangeFrom,
            DateTime toExclusive,
            Guid? authorId,
            Guid? categoryId) =>
            from rental in context.Rentals.AsNoTracking()
            where rental.StartDate >= rangeFrom && rental.StartDate < toExclusive
            where !authorId.HasValue || rental.RentalCopies.Any(copy =>
                copy.BookCopy != null && copy.BookCopy.Book.AuthorId == authorId.Value)
            where !categoryId.HasValue || rental.RentalCopies.Any(copy =>
                copy.BookCopy != null && copy.BookCopy.Book.CategoryId == categoryId.Value)
            group rental by new { rental.StartDate.Year, rental.StartDate.Month } into grouped
            select new ReportQueryHelpers.MonthCount
            {
                Year = grouped.Key.Year,
                Month = grouped.Key.Month,
                Count = grouped.Count()
            };

        internal static IQueryable<ReportQueryHelpers.MonthCount> SubscriptionMonths(
            FreeBookDbContext context,
            DateTime rangeFrom,
            DateTime toExclusive,
            Guid? governorateId) =>
            from subscription in context.Subscriptions.AsNoTracking()
            where !governorateId.HasValue || subscription.Subscriber.GovernorateId == governorateId.Value
            where subscription.StartDate >= rangeFrom && subscription.StartDate < toExclusive
            group subscription by new { subscription.StartDate.Year, subscription.StartDate.Month } into grouped
            select new ReportQueryHelpers.MonthCount
            {
                Year = grouped.Key.Year,
                Month = grouped.Key.Month,
                Count = grouped.Count()
            };

        internal static IQueryable<NamedCount> TopRentedBooksForBooks(
            FreeBookDbContext context,
            DateTime rangeFrom,
            DateTime toExclusive,
            List<Guid> bookIds) =>
            from rentalCopy in context.RentalCopies.AsNoTracking()
            join rental in context.Rentals.AsNoTracking() on rentalCopy.RentalId equals rental.Id
            join bookCopy in context.BookCopies.AsNoTracking() on rentalCopy.BookCopyId equals bookCopy.Id
            join book in Books(context) on bookCopy.BookId equals book.Id
            where rental.StartDate >= rangeFrom && rental.StartDate < toExclusive
            where bookIds.Contains(book.Id)
            group book by new { book.Id, book.Name } into grouped
            select new NamedCount
            {
                Name = grouped.Key.Name,
                Count = grouped.Count()
            };

        internal static IQueryable<ReportQueryHelpers.MonthCount> RentalMonthsForBooks(
            FreeBookDbContext context,
            DateTime rangeFrom,
            DateTime toExclusive,
            List<Guid> bookIds) =>
            from rental in context.Rentals.AsNoTracking()
            where rental.StartDate >= rangeFrom && rental.StartDate < toExclusive
            where rental.RentalCopies.Any(copy => copy.BookCopy != null && bookIds.Contains(copy.BookCopy.BookId))
            group rental by new { rental.StartDate.Year, rental.StartDate.Month } into grouped
            select new ReportQueryHelpers.MonthCount
            {
                Year = grouped.Key.Year,
                Month = grouped.Key.Month,
                Count = grouped.Count()
            };

    }
}
