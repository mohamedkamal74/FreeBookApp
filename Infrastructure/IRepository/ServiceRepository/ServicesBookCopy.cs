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
    public class ServicesBookCopy : IServiceRepository<BookCopy>
    {
        private readonly FreeBookDbContext _context;
        private readonly ILogger<ServicesBookCopy> _logger;

        public ServicesBookCopy(FreeBookDbContext context, ILogger<ServicesBookCopy> logger)
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
                    return Result.Failure<bool>("NOT_FOUND", $"BookCopy with id {id} not found");

                _context.BookCopies.Remove(result.Value);
                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting book copy {Id}", id);
                return Result.Failure<bool>("DELETE_ERROR", "Failed to delete book copy", ex.Message);
            }
        }

        public async Task<Result<BookCopy?>> FindByAsync(string name)
        {
            try
            {
                var trimmed = name.Trim();
                var bookCopy = await _context.BookCopies
                    .Include(x => x.Book)
                    .FirstOrDefaultAsync(x => x.Book != null && x.Book.Name.Equals(trimmed));
                return Result.Success(bookCopy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding book copy by name {Name}", name);
                return Result.Failure<BookCopy?>("FIND_ERROR", "Failed to find book copy", ex.Message);
            }
        }

        public async Task<Result<BookCopy?>> FindByIdAsync(Guid id)
        {
            try
            {
                var bookCopy = await _context.BookCopies
                    .Include(x => x.Book)
                    .FirstOrDefaultAsync(x => x.Id == id);
                return Result.Success(bookCopy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding book copy by id {Id}", id);
                return Result.Failure<BookCopy?>("FIND_ERROR", "Failed to find book copy", ex.Message);
            }
        }

        public async Task<Result<List<BookCopy>>> GetAllAsync()
        {
            try
            {
                var bookCopies = await _context.BookCopies
                    .Include(x => x.Book)
                    .OrderBy(x => x.Book != null ? x.Book.Name : "")
                    .ToListAsync();
                return Result.Success(bookCopies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all book copies");
                return Result.Failure<List<BookCopy>>("GET_ALL_ERROR", "Failed to get book copies", ex.Message);
            }
        }

        public async Task<Result<bool>> SaveAsync(BookCopy model)
        {
            try
            {
                var result = await FindByIdAsync(model.Id);
                if (!result.IsSuccess)
                    return Result.Failure<bool>("FIND_ERROR", "Failed to check existing book copy");

                if (result.Value is null)
                {
                    model.Id = Guid.NewGuid();
                    _context.BookCopies.Add(model);
                }
                else
                {
                    result.Value.BookId = model.BookId;
                    result.Value.EditionNumber = model.EditionNumber;
                    result.Value.SerialNumber = model.SerialNumber;
                    result.Value.IsAvailableForRental = model.IsAvailableForRental;
                    _context.BookCopies.Update(result.Value);
                }

                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving book copy {Id}", model.Id);
                return Result.Failure<bool>("SAVE_ERROR", "Failed to save book copy", ex.Message);
            }
        }
    }
}
