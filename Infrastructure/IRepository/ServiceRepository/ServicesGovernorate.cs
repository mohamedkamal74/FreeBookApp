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
    public class ServicesGovernorate : IServiceRepository<Governorate>
    {
        private readonly FreeBookDbContext _context;
        private readonly ILogger<ServicesGovernorate> _logger;

        public ServicesGovernorate(FreeBookDbContext context, ILogger<ServicesGovernorate> logger)
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
                    return Result.Failure<bool>("NOT_FOUND", $"Governorate with id {id} not found");

                // Check if governorate has areas or subscribers
                var hasAreas = await _context.Areas.AnyAsync(a => a.GovernorateId == id);
                var hasSubscribers = await _context.Subscribers.AnyAsync(s => s.GovernorateId == id);

                if (hasAreas || hasSubscribers)
                    return Result.Failure<bool>("REFERENCE_EXISTS", "Cannot delete governorate with associated areas or subscribers");

                _context.Governorates.Remove(result.Value);
                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting governorate {Id}", id);
                return Result.Failure<bool>("DELETE_ERROR", "Failed to delete governorate", ex.Message);
            }
        }

        public async Task<Result<Governorate?>> FindByAsync(string name)
        {
            try
            {
                var trimmed = name.Trim();
                var governorate = await _context.Governorates
                    .FirstOrDefaultAsync(x => x.Name.Equals(trimmed));
                return Result.Success(governorate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding governorate by name {Name}", name);
                return Result.Failure<Governorate?>("FIND_ERROR", "Failed to find governorate", ex.Message);
            }
        }

        public async Task<Result<Governorate?>> FindByIdAsync(Guid id)
        {
            try
            {
                var governorate = await _context.Governorates
                    .Include(x => x.Areas)
                    .FirstOrDefaultAsync(x => x.Id == id);
                return Result.Success(governorate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding governorate by id {Id}", id);
                return Result.Failure<Governorate?>("FIND_ERROR", "Failed to find governorate", ex.Message);
            }
        }

        public async Task<Result<List<Governorate>>> GetAllAsync()
        {
            try
            {
                var governorates = await _context.Governorates
                    .Include(x => x.Areas)
                    .OrderBy(x => x.Name)
                    .ToListAsync();
                return Result.Success(governorates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all governorates");
                return Result.Failure<List<Governorate>>("GET_ALL_ERROR", "Failed to get governorates", ex.Message);
            }
        }

        public async Task<Result<bool>> SaveAsync(Governorate model)
        {
            try
            {
                var result = await FindByIdAsync(model.Id);
                if (!result.IsSuccess)
                    return Result.Failure<bool>("FIND_ERROR", "Failed to check existing governorate");

                if (result.Value is null)
                {
                    model.Id = Guid.NewGuid();
                    _context.Governorates.Add(model);
                }
                else
                {
                    result.Value.Name = model.Name;
                    _context.Governorates.Update(result.Value);
                }

                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving governorate {Id}", model.Id);
                return Result.Failure<bool>("SAVE_ERROR", "Failed to save governorate", ex.Message);
            }
        }
    }
}
