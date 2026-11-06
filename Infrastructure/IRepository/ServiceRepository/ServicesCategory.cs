using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domin.Entity;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IRepository.ServiceRepository
{
    public class ServicesCategory : IServiceRepository<Category>
    {
        private readonly FreeBookDbContext _context;

        public ServicesCategory(FreeBookDbContext context)
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

                result.Currentstste = (int)Helper.ECurrentState.Delete;
                _context.categories.Update(result);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<Category?> FindByAsync(string name)
        {
            try
            {
                var trimmed = name.Trim();
                return await _context.categories
                    .FirstOrDefaultAsync(x => x.Name.Equals(trimmed) && x.Currentstste > 0);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<Category?> FindByIdAsync(Guid id)
        {
            try
            {
                return await _context.categories
                    .FirstOrDefaultAsync(x => x.Id == id && x.Currentstste > 0);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<Category>> GetAllAsync()
        {
            try
            {
                return await _context.categories
                    .OrderBy(x => x.Name)
                    .Where(x => x.Currentstste > 0)
                    .ToListAsync();
            }
            catch (Exception)
            {
                return new List<Category>();
            }
        }

        public async Task<bool> SaveAsync(Category model)
        {
            try
            {
                var result = await FindByIdAsync(model.Id);
                if (result is null)
                {
                    model.Id = Guid.NewGuid();
                    model.Currentstste = (int)Helper.ECurrentState.Active;
                    _context.categories.Add(model);
                }
                else
                {
                    result.Name = model.Name;
                    result.Description = model.Description;
                    result.Currentstste = (int)Helper.ECurrentState.Active;
                    _context.categories.Update(result);
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
