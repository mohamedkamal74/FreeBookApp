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
    public class ServicesCategory : IServiceRepository<Category>
    {
        private readonly FreeBookDbContext _context;
        private readonly ILogger<ServicesCategory> _logger;

        public ServicesCategory(FreeBookDbContext context, ILogger<ServicesCategory> logger)
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
                    return Result.Failure<bool>("NOT_FOUND", $"Category with id {id} not found");

                result.Value.Currentstste = (int)Helper.ECurrentState.Delete;
                _context.categories.Update(result.Value);
                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category {Id}", id);
                return Result.Failure<bool>("DELETE_ERROR", "Failed to delete category", ex.Message);
            }
        }

        public async Task<Result<Category?>> FindByAsync(string name)
        {
            try
            {
                var trimmed = name.Trim();
                var category = await _context.categories
                    .FirstOrDefaultAsync(x => x.Name.Equals(trimmed) && x.Currentstste > 0);
                return Result.Success(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding category by name {Name}", name);
                return Result.Failure<Category?>("FIND_ERROR", "Failed to find category", ex.Message);
            }
        }

        public async Task<Result<Category?>> FindByIdAsync(Guid id)
        {
            try
            {
                var category = await _context.categories
                    .FirstOrDefaultAsync(x => x.Id == id && x.Currentstste > 0);
                return Result.Success(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding category by id {Id}", id);
                return Result.Failure<Category?>("FIND_ERROR", "Failed to find category", ex.Message);
            }
        }

        public async Task<Result<List<Category>>> GetAllAsync()
        {
            try
            {
                var categories = await _context.categories
                    .OrderBy(x => x.Name)
                    .Where(x => x.Currentstste > 0)
                    .ToListAsync();
                return Result.Success(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all categories");
                return Result.Failure<List<Category>>("GET_ALL_ERROR", "Failed to get categories", ex.Message);
            }
        }

        public async Task<Result<bool>> SaveAsync(Category model)
        {
            try
            {
                var result = await FindByIdAsync(model.Id);
                if (!result.IsSuccess)
                    return Result.Failure<bool>("FIND_ERROR", "Failed to check existing category");

                if (result.Value is null)
                {
                    model.Id = Guid.NewGuid();
                    model.Currentstste = (int)Helper.ECurrentState.Active;
                    _context.categories.Add(model);
                }
                else
                {
                    result.Value.Name = model.Name;
                    result.Value.Description = model.Description;
                    result.Value.Currentstste = (int)Helper.ECurrentState.Active;
                    _context.categories.Update(result.Value);
                }

                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving category {Id}", model.Id);
                return Result.Failure<bool>("SAVE_ERROR", "Failed to save category", ex.Message);
            }
        }
    }
}
