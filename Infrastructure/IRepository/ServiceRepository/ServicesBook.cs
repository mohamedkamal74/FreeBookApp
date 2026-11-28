using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domin.Entity;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.IRepository.ServiceRepository
{
    public class ServicesBook : IBookRepository
    {
        private readonly FreeBookDbContext _context;
        private readonly ILogger<ServicesBook> _logger;

        public ServicesBook(FreeBookDbContext context, ILogger<ServicesBook> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Result<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var book = await _context.books
                    .FirstOrDefaultAsync(x => x.Id == id && x.CurrentState > 0);
                if (book is null)
                    return Result.Failure<bool>("NOT_FOUND", $"Book with id {id} not found");

                book.CurrentState = (int)Helper.ECurrentState.Delete;
                _context.books.Update(book);
                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting book {Id}", id);
                return Result.Failure<bool>("DELETE_ERROR", "Failed to delete book", ex.Message);
            }
        }

        public async Task<Result<BookDto?>> FindByAsync(string name)
        {
            try
            {
                var trimmed = name.Trim();
                var book = await _context.books
                    .Include(x => x.Author)
                    .Include(x => x.Category)
                    .Include(x => x.SubCategory)
                    .FirstOrDefaultAsync(x => x.Name.Equals(trimmed) && x.CurrentState > 0);
                return Result.Success<BookDto?>(book is null ? null : CrudMapping.ToDto(book));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding book by name {Name}", name);
                return Result.Failure<BookDto?>("FIND_ERROR", "Failed to find book", ex.Message);
            }
        }

        public async Task<Result<BookDto?>> FindByIdAsync(Guid id)
        {
            try
            {
                var book = await _context.books
                    .Include(x => x.Author)
                    .Include(x => x.Category)
                    .Include(x => x.SubCategory)
                    .FirstOrDefaultAsync(x => x.Id == id && x.CurrentState > 0);
                return Result.Success<BookDto?>(book is null ? null : CrudMapping.ToDto(book));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding book by id {Id}", id);
                return Result.Failure<BookDto?>("FIND_ERROR", "Failed to find book", ex.Message);
            }
        }

        public async Task<Result<List<BookDto>>> GetAllAsync()
        {
            try
            {
                var books = await _context.books
                    .Include(x => x.Author)
                    .Include(x => x.Category)
                    .Include(x => x.SubCategory)
                    .Where(x => x.CurrentState > 0)
                    .OrderBy(x => x.Name)
                    .ToListAsync();
                return Result.Success(books.Select(CrudMapping.ToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all books");
                return Result.Failure<List<BookDto>>("GET_ALL_ERROR", "Failed to get books", ex.Message);
            }
        }

        public async Task<Result<List<BookDto>>> GetFilteredAsync(Guid? authorId, Guid? categoryId, string? filter)
        {
            try
            {
                var query = DashboardReportQueries.ActiveBooks(_context, authorId, categoryId);
                if (ListFilters.Matches(filter, ListFilters.WithoutCopies))
                {
                    query = query.Where(book => !book.Copies.Any());
                }

                var books = await query
                    .Include(book => book.Author)
                    .Include(book => book.Category)
                    .Include(book => book.SubCategory)
                    .OrderBy(book => book.Name)
                    .ToListAsync();
                return Result.Success(books.Select(CrudMapping.ToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filtered books");
                return Result.Failure<List<BookDto>>("GET_FILTERED_ERROR", "Failed to get filtered books", ex.Message);
            }
        }

        public async Task<Result<bool>> SaveAsync(BookCommand command, Guid? id = null)
        {
            try
            {
                if (id is null || id == Guid.Empty)
                {
                    var book = new Book
                    {
                        Id = Guid.NewGuid(),
                        Name = command.Name,
                        AuthorId = command.AuthorId,
                        Publisher = command.Publisher,
                        PublishingDate = command.PublishingDate,
                        ImageUrl = command.ImageUrl,
                        ImageThumbnailUrl = command.ImageThumbnailUrl,
                        ImagePublicId = command.ImagePublicId,
                        Hall = command.Hall,
                        IsAvailableForRental = command.IsAvailableForRental,
                        Description = command.Description,
                        CategoryId = command.CategoryId,
                        SubCategoryId = command.SubCategoryId,
                        CurrentState = (int)Helper.ECurrentState.Active
                    };
                    _context.books.Add(book);
                }
                else
                {
                    var book = await _context.books
                        .FirstOrDefaultAsync(x => x.Id == id.Value && x.CurrentState > 0);
                    if (book is null)
                        return Result.Failure<bool>("NOT_FOUND", $"Book with id {id} not found");

                    book.Name = command.Name;
                    book.AuthorId = command.AuthorId;
                    book.Publisher = command.Publisher;
                    book.PublishingDate = command.PublishingDate;
                    book.ImageUrl = command.ImageUrl;
                    book.ImageThumbnailUrl = command.ImageThumbnailUrl;
                    book.ImagePublicId = command.ImagePublicId;
                    book.Hall = command.Hall;
                    book.IsAvailableForRental = command.IsAvailableForRental;
                    book.Description = command.Description;
                    book.CategoryId = command.CategoryId;
                    book.SubCategoryId = command.SubCategoryId;
                    book.CurrentState = (int)Helper.ECurrentState.Active;
                    _context.books.Update(book);
                }

                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving book {Id}", id);
                return Result.Failure<bool>("SAVE_ERROR", "Failed to save book", ex.Message);
            }
        }
    }
}
