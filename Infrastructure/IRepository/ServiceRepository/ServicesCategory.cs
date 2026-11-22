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
    public class ServicesCategory : IServiceRepository<CategoryDto, CategoryCommand>
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
                var category = await _context.categories
                    .FirstOrDefaultAsync(x => x.Id == id && x.Currentstste > 0);
                if (category is null)
                    return Result.Failure<bool>("NOT_FOUND", $"Category with id {id} not found");

                category.Currentstste = (int)Helper.ECurrentState.Delete;
                _context.categories.Update(category);
                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category {Id}", id);
                return Result.Failure<bool>("DELETE_ERROR", "Failed to delete category", ex.Message);
            }
        }

        public async Task<Result<CategoryDto?>> FindByAsync(string name)
        {
            try
            {
                var trimmed = name.Trim();
                var category = await _context.categories
                    .FirstOrDefaultAsync(x => x.Name.Equals(trimmed) && x.Currentstste > 0);
                return Result.Success<CategoryDto?>(category is null ? null : CrudMapping.ToDto(category));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding category by name {Name}", name);
                return Result.Failure<CategoryDto?>("FIND_ERROR", "Failed to find category", ex.Message);
            }
        }

        public async Task<Result<CategoryDto?>> FindByIdAsync(Guid id)
        {
            try
            {
                var category = await _context.categories
                    .FirstOrDefaultAsync(x => x.Id == id && x.Currentstste > 0);
                return Result.Success<CategoryDto?>(category is null ? null : CrudMapping.ToDto(category));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding category by id {Id}", id);
                return Result.Failure<CategoryDto?>("FIND_ERROR", "Failed to find category", ex.Message);
            }
        }

        public async Task<Result<List<CategoryDto>>> GetAllAsync()
        {
            try
            {
                var categories = await _context.categories
                    .OrderBy(x => x.Name)
                    .Where(x => x.Currentstste > 0)
                    .ToListAsync();
                return Result.Success(categories.Select(CrudMapping.ToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all categories");
                return Result.Failure<List<CategoryDto>>("GET_ALL_ERROR", "Failed to get categories", ex.Message);
            }
        }

        public async Task<Result<bool>> SaveAsync(CategoryCommand command, Guid? id = null)
        {
            try
            {
                if (id is null || id == Guid.Empty)
                {
                    var category = new Category
                    {
                        Id = Guid.NewGuid(),
                        Name = command.Name,
                        Description = command.Description,
                        Currentstste = (int)Helper.ECurrentState.Active
                    };
                    _context.categories.Add(category);
                }
                else
                {
                    var category = await _context.categories
                        .FirstOrDefaultAsync(x => x.Id == id.Value && x.Currentstste > 0);
                    if (category is null)
                        return Result.Failure<bool>("NOT_FOUND", $"Category with id {id} not found");

                    category.Name = command.Name;
                    category.Description = command.Description;
                    category.Currentstste = (int)Helper.ECurrentState.Active;
                    _context.categories.Update(category);
                }

                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving category {Id}", id);
                return Result.Failure<bool>("SAVE_ERROR", "Failed to save category", ex.Message);
            }
        }
    }
}
