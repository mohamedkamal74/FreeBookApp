using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domin.Entity;
using Infrastructure.Common;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.IRepository.ServiceRepository
{
    public class ServicesBook : IServiceRepository<Book>
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
                var result = await FindByIdAsync(id);
                if (!result.IsSuccess || result.Value is null)
                    return Result.Failure<bool>("NOT_FOUND", $"Book with id {id} not found");

                result.Value.CurrentState = (int)Helper.ECurrentState.Delete;
                _context.books.Update(result.Value);
                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting book {Id}", id);
                return Result.Failure<bool>("DELETE_ERROR", "Failed to delete book", ex.Message);
            }
        }

        public async Task<Result<Book?>> FindByAsync(string name)
        {
            try
            {
                var trimmed = name.Trim();
                var book = await _context.books
                    .Include(x => x.Author)
                    .Include(x => x.Category)
                    .FirstOrDefaultAsync(x => x.Name.Equals(trimmed) && x.CurrentState > 0);
                return Result.Success(book);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding book by name {Name}", name);
                return Result.Failure<Book?>("FIND_ERROR", "Failed to find book", ex.Message);
            }
        }

        public async Task<Result<Book?>> FindByIdAsync(Guid id)
        {
            try
            {
                var book = await _context.books
                    .Include(x => x.Author)
                    .Include(x => x.Category)
                    .Include(x => x.SubCategory)
                    .FirstOrDefaultAsync(x => x.Id == id && x.CurrentState > 0);
                return Result.Success(book);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding book by id {Id}", id);
                return Result.Failure<Book?>("FIND_ERROR", "Failed to find book", ex.Message);
            }
        }

        public async Task<Result<List<Book>>> GetAllAsync()
        {
            try
            {
                var books = await _context.books
                    .Include(x => x.Author)
                    .Include(x => x.Category)
                    .Where(x => x.CurrentState > 0)
                    .OrderBy(x => x.Name)
                    .ToListAsync();
                return Result.Success(books);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all books");
                return Result.Failure<List<Book>>("GET_ALL_ERROR", "Failed to get books", ex.Message);
            }
        }

        public async Task<Result<bool>> SaveAsync(Book model)
        {
            try
            {
                var result = await FindByIdAsync(model.Id);
                if (!result.IsSuccess)
                    return Result.Failure<bool>("FIND_ERROR", "Failed to check existing book");

                if (result.Value is null)
                {
                    model.Id = Guid.NewGuid();
                    model.CurrentState = (int)Helper.ECurrentState.Active;
                    _context.books.Add(model);
                }
                else
                {
                    result.Value.Name = model.Name;
                    result.Value.AuthorId = model.AuthorId;
                    result.Value.Publisher = model.Publisher;
                    result.Value.PublishingDate = model.PublishingDate;
                    result.Value.ImageUrl = model.ImageUrl;
                    result.Value.ImageThumbnailUrl = model.ImageThumbnailUrl;
                    result.Value.ImagePublicId = model.ImagePublicId;
                    result.Value.Hall = model.Hall;
                    result.Value.IsAvailableForRental = model.IsAvailableForRental;
                    result.Value.Description = model.Description;
                    result.Value.CategoryId = model.CategoryId;
                    result.Value.SubCategoryId = model.SubCategoryId;
                    result.Value.CurrentState = (int)Helper.ECurrentState.Active;
                    _context.books.Update(result.Value);
                }

                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving book {Id}", model.Id);
                return Result.Failure<bool>("SAVE_ERROR", "Failed to save book", ex.Message);
            }
        }
    }
}
