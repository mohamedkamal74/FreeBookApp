using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domin.Entity;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IRepository.ServiceRepository
{
    public class ServicesGovernorate : IServiceRepository<Governorate>
    {
        private readonly FreeBookDbContext _context;

        public ServicesGovernorate(FreeBookDbContext context)
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

                // Check if governorate has areas or subscribers
                var hasAreas = await _context.Areas.AnyAsync(a => a.GovernorateId == id);
                var hasSubscribers = await _context.Subscribers.AnyAsync(s => s.GovernorateId == id);

                if (hasAreas || hasSubscribers)
                    return false; // Cannot delete if referenced

                _context.Governorates.Remove(result);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<Governorate?> FindByAsync(string name)
        {
            try
            {
                var trimmed = name.Trim();
                return await _context.Governorates
                    .FirstOrDefaultAsync(x => x.Name.Equals(trimmed));
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<Governorate?> FindByIdAsync(Guid id)
        {
            try
            {
                return await _context.Governorates
                    .Include(x => x.Areas)
                    .FirstOrDefaultAsync(x => x.Id == id);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<Governorate>> GetAllAsync()
        {
            try
            {
                return await _context.Governorates
                    .Include(x => x.Areas)
                    .OrderBy(x => x.Name)
                    .ToListAsync();
            }
            catch (Exception)
            {
                return new List<Governorate>();
            }
        }

        public async Task<bool> SaveAsync(Governorate model)
        {
            try
            {
                var result = await FindByIdAsync(model.Id);
                if (result is null)
                {
                    model.Id = Guid.NewGuid();
                    _context.Governorates.Add(model);
                }
                else
                {
                    result.Name = model.Name;
                    _context.Governorates.Update(result);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
