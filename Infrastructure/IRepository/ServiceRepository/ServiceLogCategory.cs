using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domin.Entity;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IRepository.ServiceRepository
{
    public class ServiceLogCategory : IServiceRepositoryLog<LogCategory>
    {
        private readonly FreeBookDbContext _context;

        public ServiceLogCategory(FreeBookDbContext context)
        {
            _context = context;
        }

        public async Task<bool> DeleteAsync(Guid id, Guid userId)
        {
            return await AddLogAsync(Helper.Delete, id, userId);
        }

        public async Task<bool> DeleteLogAsync(Guid id)
        {
            try
            {
                var result = await FindByIdAsync(id);
                if (result is null)
                    return false;

                _context.Logcategories.Remove(result);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<LogCategory?> FindByIdAsync(Guid id)
        {
            try
            {
                return await _context.Logcategories
                    .Include(x => x.category)
                    .FirstOrDefaultAsync(x => x.Id == id);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<LogCategory>> GetAllAsync()
        {
            try
            {
                return await _context.Logcategories
                    .Include(x => x.category)
                    .OrderByDescending(x => x.Date)
                    .ToListAsync();
            }
            catch (Exception)
            {
                return new List<LogCategory>();
            }
        }

        public async Task<bool> SaveAsync(Guid id, Guid userId)
        {
            return await AddLogAsync(Helper.Save, id, userId);
        }

        public async Task<bool> UpdateAsync(Guid id, Guid userId)
        {
            return await AddLogAsync(Helper.Update, id, userId);
        }

        private async Task<bool> AddLogAsync(string action, Guid categoryId, Guid userId)
        {
            try
            {
                var logCategory = new LogCategory
                {
                    Id = Guid.NewGuid(),
                    Action = action,
                    UserId = userId,
                    Date = DateTime.Now,
                    CategoryId = categoryId
                };
                _context.Logcategories.Add(logCategory);
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
