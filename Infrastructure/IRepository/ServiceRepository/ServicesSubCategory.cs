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
    public class ServicesSubCategory : IServiceRepository<SubCategoryDto, SubCategoryCommand>
    {
        private readonly FreeBookDbContext _context;
        private readonly ILogger<ServicesSubCategory> _logger;

        public ServicesSubCategory(FreeBookDbContext context, ILogger<ServicesSubCategory> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Result<List<SubCategoryDto>>> GetAllAsync()
        {
            try
            {
                var subCategories = await _context.Subcategories
                    .Where(x => x.Currentstate > 0)
                    .OrderBy(x => x.Name)
                    .ToListAsync();
                return Result.Success(subCategories.Select(CrudMapping.ToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all subcategories");
                return Result.Failure<List<SubCategoryDto>>("GET_ALL_ERROR", "Failed to get subcategories", ex.Message);
            }
        }

        public async Task<Result<SubCategoryDto?>> FindByIdAsync(Guid id)
        {
            try
            {
                var subCategory = await _context.Subcategories
                    .FirstOrDefaultAsync(x => x.Id == id && x.Currentstate > 0);
                return Result.Success<SubCategoryDto?>(subCategory is null ? null : CrudMapping.ToDto(subCategory));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding subcategory {Id}", id);
                return Result.Failure<SubCategoryDto?>("FIND_ERROR", "Failed to find subcategory", ex.Message);
            }
        }

        public async Task<Result<SubCategoryDto?>> FindByAsync(string name)
        {
            try
            {
                var trimmed = name.Trim();
                var subCategory = await _context.Subcategories
                    .FirstOrDefaultAsync(x => x.Name.Equals(trimmed) && x.Currentstate > 0);
                return Result.Success<SubCategoryDto?>(subCategory is null ? null : CrudMapping.ToDto(subCategory));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding subcategory by name {Name}", name);
                return Result.Failure<SubCategoryDto?>("FIND_ERROR", "Failed to find subcategory", ex.Message);
            }
        }

        public async Task<Result<bool>> SaveAsync(SubCategoryCommand command, Guid? id = null)
        {
            try
            {
                if (id is null || id == Guid.Empty)
                {
                    var subCategory = new SubCategory
                    {
                        Id = Guid.NewGuid(),
                        Name = command.Name,
                        CategoryId = command.CategoryId,
                        Currentstate = (int)Helper.ECurrentState.Active
                    };
                    _context.Subcategories.Add(subCategory);
                }
                else
                {
                    var subCategory = await _context.Subcategories
                        .FirstOrDefaultAsync(x => x.Id == id.Value && x.Currentstate > 0);
                    if (subCategory is null)
                        return Result.Failure<bool>("NOT_FOUND", $"Subcategory with id {id} not found");

                    subCategory.Name = command.Name;
                    subCategory.CategoryId = command.CategoryId;
                    subCategory.Currentstate = (int)Helper.ECurrentState.Active;
                    _context.Subcategories.Update(subCategory);
                }

                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving subcategory {Id}", id);
                return Result.Failure<bool>("SAVE_ERROR", "Failed to save subcategory", ex.Message);
            }
        }

        public async Task<Result<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var subCategory = await _context.Subcategories
                    .FirstOrDefaultAsync(x => x.Id == id && x.Currentstate > 0);
                if (subCategory is null)
                    return Result.Failure<bool>("NOT_FOUND", $"Subcategory with id {id} not found");

                subCategory.Currentstate = (int)Helper.ECurrentState.Delete;
                _context.Subcategories.Update(subCategory);
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
