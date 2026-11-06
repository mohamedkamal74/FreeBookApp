using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domin.Entity;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IRepository.ServiceRepository
{
    public class ServicesSubscriber : IServiceRepository<Subscriber>
    {
        private readonly FreeBookDbContext _context;

        public ServicesSubscriber(FreeBookDbContext context)
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

                _context.Subscribers.Remove(result);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<Subscriber?> FindByAsync(string name)
        {
            try
            {
                var trimmed = name.Trim();
                return await _context.Subscribers
                    .Include(x => x.Area)
                    .Include(x => x.Governorate)
                    .FirstOrDefaultAsync(x => (x.FirstName + " " + x.LastName).Equals(trimmed));
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<Subscriber?> FindByIdAsync(Guid id)
        {
            try
            {
                return await _context.Subscribers
                    .Include(x => x.Area)
                    .Include(x => x.Governorate)
                    .Include(x => x.Subscriptions)
                    .Include(x => x.Rentals)
                    .FirstOrDefaultAsync(x => x.Id == id);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<Subscriber>> GetAllAsync()
        {
            try
            {
                return await _context.Subscribers
                    .Include(x => x.Area)
                    .Include(x => x.Governorate)
                    .OrderBy(x => x.FirstName)
                    .ThenBy(x => x.LastName)
                    .ToListAsync();
            }
            catch (Exception)
            {
                return new List<Subscriber>();
            }
        }

        public async Task<bool> SaveAsync(Subscriber model)
        {
            try
            {
                var result = await FindByIdAsync(model.Id);
                if (result is null)
                {
                    model.Id = Guid.NewGuid();
                    _context.Subscribers.Add(model);
                }
                else
                {
                    result.FirstName = model.FirstName;
                    result.LastName = model.LastName;
                    result.DateOfBirth = model.DateOfBirth;
                    result.NationalId = model.NationalId;
                    result.MobileNumber = model.MobileNumber;
                    result.HasWhatsApp = model.HasWhatsApp;
                    result.Email = model.Email;
                    result.ImageUrl = model.ImageUrl;
                    result.ImageThumbnailUrl = model.ImageThumbnailUrl;
                    result.AreaId = model.AreaId;
                    result.GovernorateId = model.GovernorateId;
                    result.Address = model.Address;
                    result.IsBlackListed = model.IsBlackListed;
                    _context.Subscribers.Update(result);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<Subscriber?> FindByNationalIdAsync(string nationalId)
        {
            try
            {
                return await _context.Subscribers
                    .Include(x => x.Area)
                    .Include(x => x.Governorate)
                    .FirstOrDefaultAsync(x => x.NationalId == nationalId);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<Subscriber>> GetBlackListedAsync()
        {
            try
            {
                return await _context.Subscribers
                    .Include(x => x.Area)
                    .Include(x => x.Governorate)
                    .Where(x => x.IsBlackListed)
                    .OrderBy(x => x.FirstName)
                    .ToListAsync();
            }
            catch (Exception)
            {
                return new List<Subscriber>();
            }
        }

        public async Task<List<Subscriber>> GetActiveSubscribersAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                return await _context.Subscribers
                    .Include(x => x.Area)
                    .Include(x => x.Governorate)
                    .Include(x => x.Subscriptions)
                    .Where(x => !x.IsBlackListed && x.Subscriptions.Any(s => s.StartDate <= now && s.EndDate >= now))
                    .OrderBy(x => x.FirstName)
                    .ToListAsync();
            }
            catch (Exception)
            {
                return new List<Subscriber>();
            }
        }
    }
}
