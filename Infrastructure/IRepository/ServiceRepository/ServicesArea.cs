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
    public class ServicesArea : IServiceRepository<AreaDto, AreaCommand>
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
                var area = await _context.Areas
                    .FirstOrDefaultAsync(x => x.Id == id);
                if (area is null)
                    return Result.Failure<bool>("NOT_FOUND", $"Area with id {id} not found");

                // Check if area has subscribers
                var hasSubscribers = await _context.Subscribers.AnyAsync(s => s.AreaId == id);

                if (hasSubscribers)
                    return Result.Failure<bool>("REFERENCE_EXISTS", "Cannot delete area with associated subscribers");

                _context.Areas.Remove(area);
                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting area {Id}", id);
                return Result.Failure<bool>("DELETE_ERROR", "Failed to delete area", ex.Message);
            }
        }

        public async Task<Result<AreaDto?>> FindByAsync(string name)
        {
            try
            {
                var trimmed = name.Trim();
                var area = await _context.Areas
                    .Include(x => x.Governorate)
                    .FirstOrDefaultAsync(x => x.Name.Equals(trimmed));
                return Result.Success<AreaDto?>(area is null ? null : CrudMapping.ToDto(area));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding area by name {Name}", name);
                return Result.Failure<AreaDto?>("FIND_ERROR", "Failed to find area", ex.Message);
            }
        }

        public async Task<Result<AreaDto?>> FindByIdAsync(Guid id)
        {
            try
            {
                var area = await _context.Areas
                    .Include(x => x.Governorate)
                    .FirstOrDefaultAsync(x => x.Id == id);
                return Result.Success<AreaDto?>(area is null ? null : CrudMapping.ToDto(area));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding area by id {Id}", id);
                return Result.Failure<AreaDto?>("FIND_ERROR", "Failed to find area", ex.Message);
            }
        }

        public async Task<Result<List<AreaDto>>> GetAllAsync()
        {
            try
            {
                var areas = await _context.Areas
                    .Include(x => x.Governorate)
                    .OrderBy(x => x.Governorate != null ? x.Governorate.Name : "")
                    .ThenBy(x => x.Name)
                    .ToListAsync();
                return Result.Success(areas.Select(CrudMapping.ToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all areas");
                return Result.Failure<List<AreaDto>>("GET_ALL_ERROR", "Failed to get areas", ex.Message);
            }
        }

        public async Task<Result<bool>> SaveAsync(AreaCommand command, Guid? id = null)
        {
            try
            {
                if (id is null || id == Guid.Empty)
                {
                    var area = new Area
                    {
                        Id = Guid.NewGuid(),
                        Name = command.Name,
                        GovernorateId = command.GovernorateId
                    };
                    _context.Areas.Add(area);
                }
                else
                {
                    var area = await _context.Areas
                        .FirstOrDefaultAsync(x => x.Id == id.Value);
                    if (area is null)
                        return Result.Failure<bool>("NOT_FOUND", $"Area with id {id} not found");

                    area.Name = command.Name;
                    area.GovernorateId = command.GovernorateId;
                    _context.Areas.Update(area);
                }

                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving area {Id}", id);
                return Result.Failure<bool>("SAVE_ERROR", "Failed to save area", ex.Message);
            }
        }
    }
}
