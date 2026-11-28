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
    public class ServicesSubscriber : ISubscriberRepository
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
                var subscriber = await _context.Subscribers
                    .FirstOrDefaultAsync(x => x.Id == id);
                if (subscriber is null)
                    return Result.Failure<bool>("NOT_FOUND", $"Subscriber with id {id} not found");

                _context.Subscribers.Remove(subscriber);
                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subscriber {Id}", id);
                return Result.Failure<bool>("DELETE_ERROR", "Failed to delete subscriber", ex.Message);
            }
        }

        public async Task<Result<SubscriberDto?>> FindByAsync(string name)
        {
            try
            {
                var trimmed = name.Trim();
                var subscriber = await _context.Subscribers
                    .Include(x => x.Area)
                    .Include(x => x.Governorate)
                    .Include(x => x.Subscriptions)
                    .FirstOrDefaultAsync(x => (x.FirstName + " " + x.LastName).Equals(trimmed));
                return Result.Success<SubscriberDto?>(subscriber is null ? null : CrudMapping.ToDto(subscriber));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding subscriber by name {Name}", name);
                return Result.Failure<SubscriberDto?>("FIND_ERROR", "Failed to find subscriber", ex.Message);
            }
        }

        public async Task<Result<SubscriberDto?>> FindByIdAsync(Guid id)
        {
            try
            {
                var subscriber = await _context.Subscribers
                    .Include(x => x.Area)
                    .Include(x => x.Governorate)
                    .Include(x => x.Subscriptions)
                    .Include(x => x.Rentals)
                    .FirstOrDefaultAsync(x => x.Id == id);
                return Result.Success<SubscriberDto?>(subscriber is null ? null : CrudMapping.ToDto(subscriber));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding subscriber by id {Id}", id);
                return Result.Failure<SubscriberDto?>("FIND_ERROR", "Failed to find subscriber", ex.Message);
            }
        }

        public async Task<Result<List<SubscriberDto>>> GetAllAsync()
        {
            try
            {
                var subscribers = await _context.Subscribers
                    .Include(x => x.Area)
                    .Include(x => x.Governorate)
                    .Include(x => x.Subscriptions)
                    .OrderBy(x => x.FirstName)
                    .ThenBy(x => x.LastName)
                    .ToListAsync();
                return Result.Success(subscribers.Select(CrudMapping.ToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all subscribers");
                return Result.Failure<List<SubscriberDto>>("GET_ALL_ERROR", "Failed to get subscribers", ex.Message);
            }
        }

        public async Task<Result<List<SubscriberDto>>> GetBlacklistedSubscribersAsync()
        {
            try
            {
                var subscribers = await _context.Subscribers
                    .Include(x => x.Area)
                    .Include(x => x.Governorate)
                    .Include(x => x.Subscriptions)
                    .Where(x => x.IsBlackListed)
                    .OrderBy(x => x.FirstName)
                    .ThenBy(x => x.LastName)
                    .ToListAsync();
                return Result.Success(subscribers.Select(CrudMapping.ToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blacklisted subscribers");
                return Result.Failure<List<SubscriberDto>>("GET_BLACKLISTED_ERROR", "Failed to get blacklisted subscribers", ex.Message);
            }
        }

        public async Task<Result<List<SubscriberDto>>> GetFilteredAsync(string? filter)
        {
            return ListFilters.Matches(filter, ListFilters.Blacklisted)
                ? await GetBlacklistedSubscribersAsync()
                : await GetAllAsync();
        }

        public async Task<Result<bool>> SaveAsync(SubscriberCommand command, Guid? id = null)
        {
            try
            {
                if (id is null || id == Guid.Empty)
                {
                    var subscriber = new Subscriber
                    {
                        Id = Guid.NewGuid(),
                        FirstName = command.FirstName,
                        LastName = command.LastName,
                        DateOfBirth = command.DateOfBirth,
                        NationalId = command.NationalId,
                        MobileNumber = command.MobileNumber,
                        HasWhatsApp = command.HasWhatsApp,
                        Email = command.Email,
                        ImageUrl = command.ImageUrl,
                        ImageThumbnailUrl = command.ImageThumbnailUrl,
                        AreaId = command.AreaId,
                        GovernorateId = command.GovernorateId,
                        Address = command.Address,
                        IsBlackListed = command.IsBlackListed
                    };
                    _context.Subscribers.Add(subscriber);
                }
                else
                {
                    var subscriber = await _context.Subscribers
                        .FirstOrDefaultAsync(x => x.Id == id.Value);
                    if (subscriber is null)
                        return Result.Failure<bool>("NOT_FOUND", $"Subscriber with id {id} not found");

                    subscriber.FirstName = command.FirstName;
                    subscriber.LastName = command.LastName;
                    subscriber.DateOfBirth = command.DateOfBirth;
                    subscriber.NationalId = command.NationalId;
                    subscriber.MobileNumber = command.MobileNumber;
                    subscriber.HasWhatsApp = command.HasWhatsApp;
                    subscriber.Email = command.Email;
                    subscriber.ImageUrl = command.ImageUrl;
                    subscriber.ImageThumbnailUrl = command.ImageThumbnailUrl;
                    subscriber.AreaId = command.AreaId;
                    subscriber.GovernorateId = command.GovernorateId;
                    subscriber.Address = command.Address;
                    subscriber.IsBlackListed = command.IsBlackListed;
                    _context.Subscribers.Update(subscriber);
                }

                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving subscriber {Id}", id);
                return Result.Failure<bool>("SAVE_ERROR", "Failed to save subscriber", ex.Message);
            }
        }
    }
}
