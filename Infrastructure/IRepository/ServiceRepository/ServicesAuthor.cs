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
    public class ServicesAuthor : IAuthorRepository
    {
        private readonly FreeBookDbContext _context;
        private readonly ILogger<ServicesAuthor> _logger;

        public ServicesAuthor(FreeBookDbContext context, ILogger<ServicesAuthor> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Result<List<Author>>> GetAllActiveAsync()
        {
            try
            {
                var authors = await _context.Authors
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.Name)
                    .ToListAsync();
                return Result.Success(authors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all active authors");
                return Result.Failure<List<Author>>("GET_ALL_ERROR", "Failed to get authors", ex.Message);
            }
        }

        public async Task<Result<Author?>> FindByNameAsync(string name)
        {
            try
            {
                var trimmed = name.Trim();
                var author = await _context.Authors
                    .FirstOrDefaultAsync(x => x.Name.Equals(trimmed) && !x.IsDeleted);
                return Result.Success(author);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding author by name {Name}", name);
                return Result.Failure<Author?>("FIND_ERROR", "Failed to find author", ex.Message);
            }
        }

        public async Task<Result<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var result = await FindByIdAsync(id);
                if (!result.IsSuccess || result.Value is null)
                    return Result.Failure<bool>("NOT_FOUND", $"Author with id {id} not found");

                result.Value.IsDeleted = true;
                _context.Authors.Update(result.Value);
                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting author {Id}", id);
                return Result.Failure<bool>("DELETE_ERROR", "Failed to delete author", ex.Message);
            }
        }

        public async Task<Result<Author?>> FindByAsync(string name)
        {
            return await FindByNameAsync(name);
        }

        public async Task<Result<Author?>> FindByIdAsync(Guid id)
        {
            try
            {
                var author = await _context.Authors
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
                return Result.Success(author);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding author by id {Id}", id);
                return Result.Failure<Author?>("FIND_ERROR", "Failed to find author", ex.Message);
            }
        }

        public async Task<Result<List<Author>>> GetAllAsync()
        {
            try
            {
                var authors = await _context.Authors
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.Name)
                    .ToListAsync();
                return Result.Success(authors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all authors");
                return Result.Failure<List<Author>>("GET_ALL_ERROR", "Failed to get authors", ex.Message);
            }
        }

        public async Task<Result<bool>> SaveAsync(Author model)
        {
            try
            {
                var result = await FindByIdAsync(model.Id);
                if (!result.IsSuccess)
                    return Result.Failure<bool>("FIND_ERROR", "Failed to check existing author");

                if (result.Value is null)
                {
                    model.Id = Guid.NewGuid();
                    model.CreatedOn = DateTime.UtcNow;
                    _context.Authors.Add(model);
                }
                else
                {
                    result.Value.Name = model.Name;
                    result.Value.LastUpdatedOn = DateTime.UtcNow;
                    _context.Authors.Update(result.Value);
                }

                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving author {Id}", model.Id);
                return Result.Failure<bool>("SAVE_ERROR", "Failed to save author", ex.Message);
            }
        }
    }
}
