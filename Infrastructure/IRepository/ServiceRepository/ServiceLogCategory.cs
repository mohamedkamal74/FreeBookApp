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
    public class ServiceLogCategory : IServiceRepositoryLog<LogCategory>
    {
        private readonly FreeBookDbContext _context;
        private readonly ILogger<ServiceLogCategory> _logger;

        public ServiceLogCategory(FreeBookDbContext context, ILogger<ServiceLogCategory> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Result<bool>> DeleteAsync(Guid id, Guid userId)
        {
            return await AddLogAsync(Helper.Delete, id, userId);
        }

        public async Task<Result<bool>> DeleteLogAsync(Guid id)
        {
            try
            {
                var result = await FindByIdAsync(id);
                if (!result.IsSuccess || result.Value is null)
                    return Result.Failure<bool>("NOT_FOUND", $"LogCategory with id {id} not found");

                _context.Logcategories.Remove(result.Value);
                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting log category {Id}", id);
                return Result.Failure<bool>("DELETE_ERROR", "Failed to delete log category", ex.Message);
            }
        }

        public async Task<Result<LogCategory?>> FindByIdAsync(Guid id)
        {
            try
            {
                var logCategory = await _context.Logcategories
                    .Include(x => x.category)
                    .FirstOrDefaultAsync(x => x.Id == id);
                return Result.Success(logCategory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding log category by id {Id}", id);
                return Result.Failure<LogCategory?>("FIND_ERROR", "Failed to find log category", ex.Message);
            }
        }

        public async Task<Result<List<LogCategory>>> GetAllAsync()
        {
            try
            {
                var logCategories = await _context.Logcategories
                    .Include(x => x.category)
                    .OrderByDescending(x => x.Date)
                    .ToListAsync();
                return Result.Success(logCategories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all log categories");
                return Result.Failure<List<LogCategory>>("GET_ALL_ERROR", "Failed to get log categories", ex.Message);
            }
        }

        public async Task<Result<bool>> SaveAsync(Guid id, Guid userId)
        {
            return await AddLogAsync(Helper.Save, id, userId);
        }

        public async Task<Result<bool>> UpdateAsync(Guid id, Guid userId)
        {
            return await AddLogAsync(Helper.Update, id, userId);
        }

        private async Task<Result<bool>> AddLogAsync(string action, Guid categoryId, Guid userId)
        {
            try
            {
                var logCategory = new LogCategory
                {
                    Id = Guid.NewGuid(),
                    Action = action,
                    UserId = userId,
                    Date = DateTime.Now,
                    CategoryId = categoryId
                };
                _context.Logcategories.Add(logCategory);
                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding log category for category {CategoryId}", categoryId);
                return Result.Failure<bool>("ADD_LOG_ERROR", "Failed to add log category", ex.Message);
            }
        }
    }
}
