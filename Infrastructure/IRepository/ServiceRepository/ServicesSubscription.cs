using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domin.Entity;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IRepository.ServiceRepository
{
    public class ServicesSubscription : IServiceRepository<Subscription>
    {
        private readonly FreeBookDbContext _context;

        public ServicesSubscription(FreeBookDbContext context)
        {
            _context = context;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var result = await FindByIdAsync(id);
                if (result is null)
                    return false;

                _context.Subscriptions.Remove(result);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<Subscription?> FindByAsync(string name)
        {
            // Not applicable for subscriptions
            return null;
        }

        public async Task<Subscription?> FindByIdAsync(Guid id)
        {
            try
            {
                return await _context.Subscriptions
                    .Include(x => x.Subscriber)
                    .FirstOrDefaultAsync(x => x.Id == id);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<Subscription>> GetAllAsync()
        {
            try
            {
                return await _context.Subscriptions
                    .Include(x => x.Subscriber)
                    .OrderByDescending(x => x.StartDate)
                    .ToListAsync();
            }
            catch (Exception)
            {
                return new List<Subscription>();
            }
        }

        public async Task<bool> SaveAsync(Subscription model)
        {
            try
            {
                var result = await FindByIdAsync(model.Id);
                if (result is null)
                {
                    model.Id = Guid.NewGuid();
                    model.CreatedOn = DateTime.UtcNow;
                    _context.Subscriptions.Add(model);
                }
                else
                {
                    result.SubscriberId = model.SubscriberId;
                    result.StartDate = model.StartDate;
                    result.EndDate = model.EndDate;
                    result.CreatedById = model.CreatedById;
                    _context.Subscriptions.Update(result);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<Subscription>> GetBySubscriberAsync(Guid subscriberId)
        {
            try
            {
                return await _context.Subscriptions
                    .Where(x => x.SubscriberId == subscriberId)
                    .OrderByDescending(x => x.StartDate)
                    .ToListAsync();
            }
            catch (Exception)
            {
                return new List<Subscription>();
            }
        }

        public async Task<List<Subscription>> GetActiveSubscriptionsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                return await _context.Subscriptions
                    .Include(x => x.Subscriber)
                    .Where(x => x.StartDate <= now && x.EndDate >= now)
                    .OrderBy(x => x.EndDate)
                    .ToListAsync();
            }
            catch (Exception)
            {
                return new List<Subscription>();
            }
        }

        public async Task<List<Subscription>> GetExpiringSubscriptionsAsync(int days = 30)
        {
            try
            {
                var now = DateTime.UtcNow;
                var future = now.AddDays(days);
                return await _context.Subscriptions
                    .Include(x => x.Subscriber)
                    .Where(x => x.EndDate >= now && x.EndDate <= future)
                    .OrderBy(x => x.EndDate)
                    .ToListAsync();
            }
            catch (Exception)
            {
                return new List<Subscription>();
            }
        }
    }
}
