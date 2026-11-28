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
    public class ServicesBookCopy : IBookCopyRepository
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
                var bookCopy = await _context.BookCopies
                    .FirstOrDefaultAsync(x => x.Id == id);
                if (bookCopy is null)
                    return Result.Failure<bool>("NOT_FOUND", $"BookCopy with id {id} not found");

                _context.BookCopies.Remove(bookCopy);
                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting book copy {Id}", id);
                return Result.Failure<bool>("DELETE_ERROR", "Failed to delete book copy", ex.Message);
            }
        }

        public async Task<Result<BookCopyDto?>> FindByAsync(string name)
        {
            try
            {
                var trimmed = name.Trim();
                var bookCopy = await _context.BookCopies
                    .Include(x => x.Book)
                    .FirstOrDefaultAsync(x => x.Book != null && x.Book.Name.Equals(trimmed));
                return Result.Success<BookCopyDto?>(bookCopy is null ? null : CrudMapping.ToDto(bookCopy));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding book copy by name {Name}", name);
                return Result.Failure<BookCopyDto?>("FIND_ERROR", "Failed to find book copy", ex.Message);
            }
        }

        public async Task<Result<BookCopyDto?>> FindByIdAsync(Guid id)
        {
            try
            {
                var bookCopy = await _context.BookCopies
                    .Include(x => x.Book)
                    .FirstOrDefaultAsync(x => x.Id == id);
                return Result.Success<BookCopyDto?>(bookCopy is null ? null : CrudMapping.ToDto(bookCopy));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding book copy by id {Id}", id);
                return Result.Failure<BookCopyDto?>("FIND_ERROR", "Failed to find book copy", ex.Message);
            }
        }

        public async Task<Result<List<BookCopyDto>>> GetAllAsync()
        {
            try
            {
                var bookCopies = await _context.BookCopies
                    .Include(x => x.Book)
                    .OrderBy(x => x.Book != null ? x.Book.Name : "")
                    .ToListAsync();
                return Result.Success(bookCopies.Select(CrudMapping.ToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all book copies");
                return Result.Failure<List<BookCopyDto>>("GET_ALL_ERROR", "Failed to get book copies", ex.Message);
            }
        }

        public async Task<Result<List<BookCopyDto>>> GetFilteredAsync(Guid? authorId, Guid? categoryId, string? filter)
        {
            try
            {
                var query = DashboardReportQueries.ActiveBookCopies(_context, authorId, categoryId);
                if (ListFilters.Matches(filter, ListFilters.Available))
                {
                    query = query.Where(copy => copy.IsAvailableForRental);
                }
                else if (ListFilters.Matches(filter, ListFilters.Rented))
                {
                    query = query.Where(copy => !copy.IsAvailableForRental);
                }

                var bookCopies = await query
                    .Include(copy => copy.Book)
                    .OrderBy(copy => copy.Book != null ? copy.Book.Name : "")
                    .ToListAsync();
                return Result.Success(bookCopies.Select(CrudMapping.ToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filtered book copies");
                return Result.Failure<List<BookCopyDto>>("GET_FILTERED_ERROR", "Failed to get filtered book copies", ex.Message);
            }
        }

        public async Task<Result<bool>> SaveAsync(BookCopyCommand command, Guid? id = null)
        {
            try
            {
                if (id is null || id == Guid.Empty)
                {
                    var bookCopy = new BookCopy
                    {
                        Id = Guid.NewGuid(),
                        BookId = command.BookId,
                        IsAvailableForRental = command.IsAvailableForRental,
                        EditionNumber = command.EditionNumber,
                        SerialNumber = command.SerialNumber
                    };
                    _context.BookCopies.Add(bookCopy);
                }
                else
                {
                    var bookCopy = await _context.BookCopies
                        .FirstOrDefaultAsync(x => x.Id == id.Value);
                    if (bookCopy is null)
                        return Result.Failure<bool>("NOT_FOUND", $"BookCopy with id {id} not found");

                    bookCopy.BookId = command.BookId;
                    bookCopy.IsAvailableForRental = command.IsAvailableForRental;
                    bookCopy.EditionNumber = command.EditionNumber;
                    bookCopy.SerialNumber = command.SerialNumber;
                    _context.BookCopies.Update(bookCopy);
                }

                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving book copy {Id}", id);
                return Result.Failure<bool>("SAVE_ERROR", "Failed to save book copy", ex.Message);
            }
        }
    }
}
