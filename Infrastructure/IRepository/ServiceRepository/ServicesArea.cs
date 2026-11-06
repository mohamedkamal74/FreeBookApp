using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domin.Entity;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IRepository.ServiceRepository
{
    public class ServicesArea : IServiceRepository<Area>
    {
        private readonly FreeBookDbContext _context;

        public ServicesArea(FreeBookDbContext context)
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

                // Check if area has subscribers
                var hasSubscribers = await _context.Subscribers.AnyAsync(s => s.AreaId == id);

                if (hasSubscribers)
                    return false; // Cannot delete if referenced

                _context.Areas.Remove(result);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<Area?> FindByAsync(string name)
        {
            try
            {
                var trimmed = name.Trim();
                return await _context.Areas
                    .Include(x => x.Governorate)
                    .FirstOrDefaultAsync(x => x.Name.Equals(trimmed));
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<Area?> FindByIdAsync(Guid id)
        {
            try
            {
                return await _context.Areas
                    .Include(x => x.Governorate)
                    .FirstOrDefaultAsync(x => x.Id == id);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<Area>> GetAllAsync()
        {
            try
            {
                return await _context.Areas
                    .Include(x => x.Governorate)
                    .OrderBy(x => x.Governorate != null ? x.Governorate.Name : "")
                    .ThenBy(x => x.Name)
                    .ToListAsync();
            }
            catch (Exception)
            {
                return new List<Area>();
            }
        }

        public async Task<bool> SaveAsync(Area model)
        {
            try
            {
                var result = await FindByIdAsync(model.Id);
                if (result is null)
                {
                    model.Id = Guid.NewGuid();
                    _context.Areas.Add(model);
                }
                else
                {
                    result.Name = model.Name;
                    result.GovernorateId = model.GovernorateId;
                    _context.Areas.Update(result);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<Area>> GetByGovernorateAsync(Guid governorateId)
        {
            try
            {
                return await _context.Areas
                    .Where(x => x.GovernorateId == governorateId)
                    .OrderBy(x => x.Name)
                    .ToListAsync();
            }
            catch (Exception)
            {
                return new List<Area>();
            }
        }
    }
}
