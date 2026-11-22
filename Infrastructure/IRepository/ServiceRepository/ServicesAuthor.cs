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
    public class ServicesAuthor : IAuthorRepository
    {
        private readonly FreeBookDbContext _context;
        private readonly ILogger<ServicesAuthor> _logger;

        public ServicesAuthor(FreeBookDbContext context, ILogger<ServicesAuthor> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Result<List<AuthorDto>>> GetAllActiveAsync()
        {
            try
            {
                var authors = await _context.Authors
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.Name)
                    .ToListAsync();
                return Result.Success(authors.Select(CrudMapping.ToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all active authors");
                return Result.Failure<List<AuthorDto>>("GET_ALL_ERROR", "Failed to get authors", ex.Message);
            }
        }

        public async Task<Result<AuthorDto?>> FindByNameAsync(string name)
        {
            try
            {
                var trimmed = name.Trim();
                var author = await _context.Authors
                    .FirstOrDefaultAsync(x => x.Name.Equals(trimmed) && !x.IsDeleted);
                return Result.Success<AuthorDto?>(author is null ? null : CrudMapping.ToDto(author));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding author by name {Name}", name);
                return Result.Failure<AuthorDto?>("FIND_ERROR", "Failed to find author", ex.Message);
            }
        }

        public async Task<Result<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var author = await _context.Authors
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
                if (author is null)
                    return Result.Failure<bool>("NOT_FOUND", $"Author with id {id} not found");

                author.IsDeleted = true;
                _context.Authors.Update(author);
                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting author {Id}", id);
                return Result.Failure<bool>("DELETE_ERROR", "Failed to delete author", ex.Message);
            }
        }

        public async Task<Result<AuthorDto?>> FindByAsync(string name)
        {
            return await FindByNameAsync(name);
        }

        public async Task<Result<AuthorDto?>> FindByIdAsync(Guid id)
        {
            try
            {
                var author = await _context.Authors
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
                return Result.Success<AuthorDto?>(author is null ? null : CrudMapping.ToDto(author));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding author by id {Id}", id);
                return Result.Failure<AuthorDto?>("FIND_ERROR", "Failed to find author", ex.Message);
            }
        }

        public async Task<Result<List<AuthorDto>>> GetAllAsync()
        {
            try
            {
                var authors = await _context.Authors
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.Name)
                    .ToListAsync();
                return Result.Success(authors.Select(CrudMapping.ToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all authors");
                return Result.Failure<List<AuthorDto>>("GET_ALL_ERROR", "Failed to get authors", ex.Message);
            }
        }

        public async Task<Result<bool>> SaveAsync(AuthorCommand command, Guid? id = null)
        {
            try
            {
                if (id is null || id == Guid.Empty)
                {
                    var author = new Author
                    {
                        Id = Guid.NewGuid(),
                        Name = command.Name,
                        CreatedOn = DateTime.UtcNow
                    };
                    _context.Authors.Add(author);
                }
                else
                {
                    var author = await _context.Authors
                        .FirstOrDefaultAsync(x => x.Id == id.Value && !x.IsDeleted);
                    if (author is null)
                        return Result.Failure<bool>("NOT_FOUND", $"Author with id {id} not found");

                    author.Name = command.Name;
                    author.LastUpdatedOn = DateTime.UtcNow;
                    _context.Authors.Update(author);
                }

                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving author {Id}", id);
                return Result.Failure<bool>("SAVE_ERROR", "Failed to save author", ex.Message);
            }
        }
    }
}
