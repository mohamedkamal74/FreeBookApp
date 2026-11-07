using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domin.Entity;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.IRepository;
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
                var result = await FindByIdAsync(id);
                if (!result.IsSuccess || result.Value is null)
                    return Result.Failure<bool>("NOT_FOUND", $"Subscription with id {id} not found");

                _context.Subscriptions.Remove(result.Value);
                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subscription {Id}", id);
                return Result.Failure<bool>("DELETE_ERROR", "Failed to delete subscription", ex.Message);
            }
        }

        public async Task<Result<Subscription?>> FindByAsync(string name)
        {
            // Not applicable for subscriptions
            return Result.Success<Subscription?>(null);
        }

        public async Task<Result<Subscription?>> FindByIdAsync(Guid id)
        {
            try
            {
                var subscription = await _context.Subscriptions
                    .Include(x => x.Subscriber)
                    .FirstOrDefaultAsync(x => x.Id == id);
                return Result.Success(subscription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding subscription by id {Id}", id);
                return Result.Failure<Subscription?>("FIND_ERROR", "Failed to find subscription", ex.Message);
            }
        }

        public async Task<Result<List<Subscription>>> GetAllAsync()
        {
            try
            {
                var subscriptions = await _context.Subscriptions
                    .Include(x => x.Subscriber)
                    .OrderByDescending(x => x.StartDate)
                    .ToListAsync();
                return Result.Success(subscriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all subscriptions");
                return Result.Failure<List<Subscription>>("GET_ALL_ERROR", "Failed to get subscriptions", ex.Message);
            }
        }

        public async Task<Result<bool>> SaveAsync(Subscription model)
        {
            try
            {
                var result = await FindByIdAsync(model.Id);
                if (!result.IsSuccess)
                    return Result.Failure<bool>("FIND_ERROR", "Failed to check existing subscription");

                if (result.Value is null)
                {
                    model.Id = Guid.NewGuid();
                    model.CreatedOn = DateTime.UtcNow;
                    _context.Subscriptions.Add(model);
                }
                else
                {
                    result.Value.SubscriberId = model.SubscriberId;
                    result.Value.StartDate = model.StartDate;
                    result.Value.EndDate = model.EndDate;
                    result.Value.CreatedById = model.CreatedById;
                    _context.Subscriptions.Update(result.Value);
                }

                await _context.SaveChangesAsync();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving subscription {Id}", model.Id);
                return Result.Failure<bool>("SAVE_ERROR", "Failed to save subscription", ex.Message);
            }
        }

        public async Task<Result<List<Subscription>>> GetBySubscriberAsync(Guid subscriberId)
        {
            try
            {
                var subscriptions = await _context.Subscriptions
                    .Where(x => x.SubscriberId == subscriberId)
                    .OrderByDescending(x => x.StartDate)
                    .ToListAsync();
                return Result.Success(subscriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscriptions for subscriber {SubscriberId}", subscriberId);
                return Result.Failure<List<Subscription>>("GET_BY_SUBSCRIBER_ERROR", "Failed to get subscriptions", ex.Message);
            }
        }

        public async Task<Result<List<Subscription>>> GetActiveSubscriptionsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var subscriptions = await _context.Subscriptions
                    .Include(x => x.Subscriber)
                    .Where(x => x.StartDate <= now && x.EndDate >= now)
                    .OrderBy(x => x.EndDate)
                    .ToListAsync();
                return Result.Success(subscriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active subscriptions");
                return Result.Failure<List<Subscription>>("GET_ACTIVE_ERROR", "Failed to get active subscriptions", ex.Message);
            }
        }

        public async Task<Result<List<Subscription>>> GetExpiringSubscriptionsAsync(int days = 30)
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
                return Result.Success(subscriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expiring subscriptions for {Days} days", days);
                return Result.Failure<List<Subscription>>("GET_EXPIRING_ERROR", "Failed to get expiring subscriptions", ex.Message);
            }
        }
    }
}
