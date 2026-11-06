using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domin.Entity;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IRepository.ServiceRepository
{
    public class ServicesAuthor : IAuthorRepository
    {
        private readonly FreeBookDbContext _context;

        public ServicesAuthor(FreeBookDbContext context)
        {
            _context = context;
        }

        public async Task<List<Author>> GetAllActiveAsync()
        {
            try
            {
                return await _context.Authors
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.Name)
                    .ToListAsync();
            }
            catch (Exception)
            {
                return new List<Author>();
            }
        }

        public async Task<Author?> FindByNameAsync(string name)
        {
            try
            {
                var trimmed = name.Trim();
                return await _context.Authors
                    .FirstOrDefaultAsync(x => x.Name.Equals(trimmed) && !x.IsDeleted);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var result = await FindByIdAsync(id);
                if (result is null)
                    return false;

                result.IsDeleted = true;
                _context.Authors.Update(result);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<Author?> FindByAsync(string name)
        {
            return await FindByNameAsync(name);
        }

        public async Task<Author?> FindByIdAsync(Guid id)
        {
            try
            {
                return await _context.Authors
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<Author>> GetAllAsync()
        {
            try
            {
                return await _context.Authors
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.Name)
                    .ToListAsync();
            }
            catch (Exception)
            {
                return new List<Author>();
            }
        }

        public async Task<bool> SaveAsync(Author model)
        {
            try
            {
                var result = await FindByIdAsync(model.Id);
                if (result is null)
                {
                    model.Id = Guid.NewGuid();
                    model.CreatedOn = DateTime.UtcNow;
                    _context.Authors.Add(model);
                }
                else
                {
                    result.Name = model.Name;
                    result.LastUpdatedOn = DateTime.UtcNow;
                    _context.Authors.Update(result);
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
