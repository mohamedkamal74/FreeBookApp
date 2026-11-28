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
    public class ServicesSubscription : ISubscriptionRepository
    {
        private readonly FreeBookDbContext _context;
        private readonly ILogger<ServicesSubscription> _logger;

        public ServicesSubscription(FreeBookDbContext context, ILogger<ServicesSubscription> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Result<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var subscription = await _context.Subscriptions
                    .FirstOrDefaultAsync(x => x.Id == id);
                if (subscription is null)
                    return Result.Failure<bool>("NOT_FOUND", $"Subscription with id {id} not found");

                _context.Subscriptions.Remove(subscription);
                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subscription {Id}", id);
                return Result.Failure<bool>("DELETE_ERROR", "Failed to delete subscription", ex.Message);
            }
        }

        public async Task<Result<SubscriptionDto?>> FindByAsync(string name)
        {
            // Not applicable for subscriptions
            return await Task.FromResult(Result.Success<SubscriptionDto?>(null));
        }

        public async Task<Result<SubscriptionDto?>> FindByIdAsync(Guid id)
        {
            try
            {
                var subscription = await _context.Subscriptions
                    .Include(x => x.Subscriber)
                    .FirstOrDefaultAsync(x => x.Id == id);
                return Result.Success<SubscriptionDto?>(subscription is null ? null : CrudMapping.ToDto(subscription));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding subscription by id {Id}", id);
                return Result.Failure<SubscriptionDto?>("FIND_ERROR", "Failed to find subscription", ex.Message);
            }
        }

        public async Task<Result<List<SubscriptionDto>>> GetAllAsync()
        {
            try
            {
                var subscriptions = await _context.Subscriptions
                    .Include(x => x.Subscriber)
                    .OrderByDescending(x => x.StartDate)
                    .ToListAsync();
                return Result.Success(subscriptions.Select(s => CrudMapping.ToDto(s)).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all subscriptions");
                return Result.Failure<List<SubscriptionDto>>("GET_ALL_ERROR", "Failed to get subscriptions", ex.Message);
            }
        }

        public async Task<Result<bool>> SaveAsync(SubscriptionCommand command, Guid? id = null)
        {
            try
            {
                if (id is null || id == Guid.Empty)
                {
                    var subscription = new Subscription
                    {
                        Id = Guid.NewGuid(),
                        SubscriberId = command.SubscriberId,
                        StartDate = command.StartDate,
                        EndDate = command.EndDate,
                        CreatedById = command.CreatedById,
                        CreatedOn = DateTime.UtcNow
                    };
                    _context.Subscriptions.Add(subscription);
                }
                else
                {
                    var subscription = await _context.Subscriptions
                        .FirstOrDefaultAsync(x => x.Id == id.Value);
                    if (subscription is null)
                        return Result.Failure<bool>("NOT_FOUND", $"Subscription with id {id} not found");

                    subscription.SubscriberId = command.SubscriberId;
                    subscription.StartDate = command.StartDate;
                    subscription.EndDate = command.EndDate;
                    subscription.CreatedById = command.CreatedById;
                    _context.Subscriptions.Update(subscription);
                }

                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving subscription {Id}", id);
                return Result.Failure<bool>("SAVE_ERROR", "Failed to save subscription", ex.Message);
            }
        }

        public async Task<Result<List<SubscriptionDto>>> GetFilteredAsync(string? filter)
        {
            if (ListFilters.Matches(filter, ListFilters.Active))
            {
                return await GetActiveSubscriptionsAsync();
            }

            if (ListFilters.Matches(filter, ListFilters.Expiring))
            {
                return await GetExpiringSubscriptionsAsync(Helper.ExpiringSubscriptionWindowDays);
            }

            return await GetAllAsync();
        }

        public async Task<Result<List<SubscriptionDto>>> GetBySubscriberAsync(Guid subscriberId)
        {
            try
            {
                var subscriptions = await _context.Subscriptions
                    .Include(x => x.Subscriber)
                    .Where(x => x.SubscriberId == subscriberId)
                    .OrderByDescending(x => x.StartDate)
                    .ToListAsync();
                return Result.Success(subscriptions.Select(s => CrudMapping.ToDto(s)).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscriptions for subscriber {SubscriberId}", subscriberId);
                return Result.Failure<List<SubscriptionDto>>("GET_BY_SUBSCRIBER_ERROR", "Failed to get subscriptions", ex.Message);
            }
        }

        public async Task<Result<List<SubscriptionDto>>> GetActiveSubscriptionsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var subscriptions = await _context.Subscriptions
                    .Include(x => x.Subscriber)
                    .Where(x => x.StartDate <= now && x.EndDate >= now)
                    .OrderBy(x => x.EndDate)
                    .ToListAsync();
                return Result.Success(subscriptions.Select(s => CrudMapping.ToDto(s)).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active subscriptions");
                return Result.Failure<List<SubscriptionDto>>("GET_ACTIVE_ERROR", "Failed to get active subscriptions", ex.Message);
            }
        }

        public async Task<Result<List<SubscriptionDto>>> GetExpiringSubscriptionsAsync(int days = 30)
        {
            try
            {
                var now = DateTime.UtcNow;
                var future = now.AddDays(days);
                var subscriptions = await _context.Subscriptions
                    .Include(x => x.Subscriber)
                    .Where(x => x.EndDate >= now && x.EndDate <= future)
                    .OrderBy(x => x.EndDate)
                    .ToListAsync();
                return Result.Success(subscriptions.Select(s => CrudMapping.ToDto(s)).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expiring subscriptions for {Days} days", days);
                return Result.Failure<List<SubscriptionDto>>("GET_EXPIRING_ERROR", "Failed to get expiring subscriptions", ex.Message);
            }
        }
    }
}
