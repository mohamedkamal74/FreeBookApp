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
    public class ServicesArea : IServiceRepository<Area>
    {
        private readonly FreeBookDbContext _context;
        private readonly ILogger<ServicesArea> _logger;

        public ServicesArea(FreeBookDbContext context, ILogger<ServicesArea> logger)
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
                    return Result.Failure<bool>("NOT_FOUND", $"Area with id {id} not found");

                // Check if area has subscribers
                var hasSubscribers = await _context.Subscribers.AnyAsync(s => s.AreaId == id);

                if (hasSubscribers)
                    return Result.Failure<bool>("REFERENCE_EXISTS", "Cannot delete area with associated subscribers");

                _context.Areas.Remove(result.Value);
                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting area {Id}", id);
                return Result.Failure<bool>("DELETE_ERROR", "Failed to delete area", ex.Message);
            }
        }

        public async Task<Result<Area?>> FindByAsync(string name)
        {
            try
            {
                var trimmed = name.Trim();
                var area = await _context.Areas
                    .Include(x => x.Governorate)
                    .FirstOrDefaultAsync(x => x.Name.Equals(trimmed));
                return Result.Success(area);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding area by name {Name}", name);
                return Result.Failure<Area?>("FIND_ERROR", "Failed to find area", ex.Message);
            }
        }

        public async Task<Result<Area?>> FindByIdAsync(Guid id)
        {
            try
            {
                var area = await _context.Areas
                    .Include(x => x.Governorate)
                    .FirstOrDefaultAsync(x => x.Id == id);
                return Result.Success(area);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding area by id {Id}", id);
                return Result.Failure<Area?>("FIND_ERROR", "Failed to find area", ex.Message);
            }
        }

        public async Task<Result<List<Area>>> GetAllAsync()
        {
            try
            {
                var areas = await _context.Areas
                    .Include(x => x.Governorate)
                    .OrderBy(x => x.Governorate != null ? x.Governorate.Name : "")
                    .ThenBy(x => x.Name)
                    .ToListAsync();
                return Result.Success(areas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all areas");
                return Result.Failure<List<Area>>("GET_ALL_ERROR", "Failed to get areas", ex.Message);
            }
        }

        public async Task<Result<bool>> SaveAsync(Area model)
        {
            try
            {
                var result = await FindByIdAsync(model.Id);
                if (!result.IsSuccess)
                    return Result.Failure<bool>("FIND_ERROR", "Failed to check existing area");

                if (result.Value is null)
                {
                    model.Id = Guid.NewGuid();
                    _context.Areas.Add(model);
                }
                else
                {
                    result.Value.Name = model.Name;
                    result.Value.GovernorateId = model.GovernorateId;
                    _context.Areas.Update(result.Value);
                }

                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving area {Id}", model.Id);
                return Result.Failure<bool>("SAVE_ERROR", "Failed to save area", ex.Message);
            }
        }
    }
}
