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
    public class ServicesSubCategory : IServiceRepository<SubCategory>
    {
        private readonly FreeBookDbContext _context;
        private readonly ILogger<ServicesSubCategory> _logger;

        public ServicesSubCategory(FreeBookDbContext context, ILogger<ServicesSubCategory> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Result<List<SubCategory>>> GetAllAsync()
        {
            try
            {
                var subCategories = await _context.Subcategories
                    .Where(x => x.Currentstate > 0)
                    .OrderBy(x => x.Name)
                    .ToListAsync();
                return Result.Success(subCategories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all subcategories");
                return Result.Failure<List<SubCategory>>("GET_ALL_ERROR", "Failed to get subcategories", ex.Message);
            }
        }

        public async Task<Result<SubCategory?>> FindByIdAsync(Guid id)
        {
            try
            {
                var subCategory = await _context.Subcategories
                    .FirstOrDefaultAsync(x => x.Id == id && x.Currentstate > 0);
                return Result.Success(subCategory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding subcategory {Id}", id);
                return Result.Failure<SubCategory?>("FIND_ERROR", "Failed to find subcategory", ex.Message);
            }
        }

        public async Task<Result<SubCategory?>> FindByAsync(string name)
        {
            try
            {
                var trimmed = name.Trim();
                var subCategory = await _context.Subcategories
                    .FirstOrDefaultAsync(x => x.Name.Equals(trimmed) && x.Currentstate > 0);
                return Result.Success(subCategory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding subcategory by name {Name}", name);
                return Result.Failure<SubCategory?>("FIND_ERROR", "Failed to find subcategory", ex.Message);
            }
        }

        public async Task<Result<bool>> SaveAsync(SubCategory model)
        {
            try
            {
                var result = await FindByIdAsync(model.Id);
                if (!result.IsSuccess)
                    return Result.Failure<bool>("FIND_ERROR", "Failed to check existing subcategory");

                if (result.Value is null)
                {
                    model.Id = Guid.NewGuid();
                    model.Currentstate = (int)Helper.ECurrentState.Active;
                    _context.Subcategories.Add(model);
                }
                else
                {
                    result.Value.Name = model.Name;
                    result.Value.CategoryId = model.CategoryId;
                    result.Value.Currentstate = (int)Helper.ECurrentState.Active;
                    _context.Subcategories.Update(result.Value);
                }

                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving subcategory {Id}", model.Id);
                return Result.Failure<bool>("SAVE_ERROR", "Failed to save subcategory", ex.Message);
            }
        }

        public async Task<Result<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var result = await FindByIdAsync(id);
                if (!result.IsSuccess || result.Value is null)
                    return Result.Failure<bool>("NOT_FOUND", $"Subcategory with id {id} not found");

                result.Value.Currentstate = (int)Helper.ECurrentState.Delete;
                _context.Subcategories.Update(result.Value);
                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subcategory {Id}", id);
                return Result.Failure<bool>("DELETE_ERROR", "Failed to delete subcategory", ex.Message);
            }
        }
    }
}
