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
    public class ServicesSubscriber : IServiceRepository<Subscriber>
    {
        private readonly FreeBookDbContext _context;
        private readonly ILogger<ServicesSubscriber> _logger;

        public ServicesSubscriber(FreeBookDbContext context, ILogger<ServicesSubscriber> logger)
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
                    return Result.Failure<bool>("NOT_FOUND", $"Subscriber with id {id} not found");

                _context.Subscribers.Remove(result.Value);
                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subscriber {Id}", id);
                return Result.Failure<bool>("DELETE_ERROR", "Failed to delete subscriber", ex.Message);
            }
        }

        public async Task<Result<Subscriber?>> FindByAsync(string name)
        {
            try
            {
                var trimmed = name.Trim();
                var subscriber = await _context.Subscribers
                    .Include(x => x.Area)
                    .Include(x => x.Governorate)
                    .FirstOrDefaultAsync(x => (x.FirstName + " " + x.LastName).Equals(trimmed));
                return Result.Success(subscriber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding subscriber by name {Name}", name);
                return Result.Failure<Subscriber?>("FIND_ERROR", "Failed to find subscriber", ex.Message);
            }
        }

        public async Task<Result<Subscriber?>> FindByIdAsync(Guid id)
        {
            try
            {
                var subscriber = await _context.Subscribers
                    .Include(x => x.Area)
                    .Include(x => x.Governorate)
                    .Include(x => x.Subscriptions)
                    .Include(x => x.Rentals)
                    .FirstOrDefaultAsync(x => x.Id == id);
                return Result.Success(subscriber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding subscriber by id {Id}", id);
                return Result.Failure<Subscriber?>("FIND_ERROR", "Failed to find subscriber", ex.Message);
            }
        }

        public async Task<Result<List<Subscriber>>> GetAllAsync()
        {
            try
            {
                var subscribers = await _context.Subscribers
                    .Include(x => x.Area)
                    .Include(x => x.Governorate)
                    .OrderBy(x => x.FirstName)
                    .ThenBy(x => x.LastName)
                    .ToListAsync();
                return Result.Success(subscribers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all subscribers");
                return Result.Failure<List<Subscriber>>("GET_ALL_ERROR", "Failed to get subscribers", ex.Message);
            }
        }

        public async Task<Result<bool>> SaveAsync(Subscriber model)
        {
            try
            {
                var result = await FindByIdAsync(model.Id);
                if (!result.IsSuccess)
                    return Result.Failure<bool>("FIND_ERROR", "Failed to check existing subscriber");

                if (result.Value is null)
                {
                    model.Id = Guid.NewGuid();
                    _context.Subscribers.Add(model);
                }
                else
                {
                    result.Value.FirstName = model.FirstName;
                    result.Value.LastName = model.LastName;
                    result.Value.DateOfBirth = model.DateOfBirth;
                    result.Value.NationalId = model.NationalId;
                    result.Value.MobileNumber = model.MobileNumber;
                    result.Value.HasWhatsApp = model.HasWhatsApp;
                    result.Value.Email = model.Email;
                    result.Value.ImageUrl = model.ImageUrl;
                    result.Value.ImageThumbnailUrl = model.ImageThumbnailUrl;
                    result.Value.AreaId = model.AreaId;
                    result.Value.GovernorateId = model.GovernorateId;
                    result.Value.Address = model.Address;
                    result.Value.IsBlackListed = model.IsBlackListed;
                    _context.Subscribers.Update(result.Value);
                }

                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving subscriber {Id}", model.Id);
                return Result.Failure<bool>("SAVE_ERROR", "Failed to save subscriber", ex.Message);
            }
        }
    }
}
