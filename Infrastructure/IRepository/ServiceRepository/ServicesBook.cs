using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domin.Entity;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IRepository.ServiceRepository
{
    public class ServicesBook : IServiceRepository<Book>
    {
        private readonly FreeBookDbContext _context;

        public ServicesBook(FreeBookDbContext context)
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

                result.CurrentState = (int)Helper.ECurrentState.Delete;
                _context.books.Update(result);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<Book?> FindByAsync(string name)
        {
            try
            {
                var trimmed = name.Trim();
                return await _context.books
                    .Include(x => x.Author)
                    .Include(x => x.Category)
                    .FirstOrDefaultAsync(x => x.Name.Equals(trimmed) && x.CurrentState > 0);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<Book?> FindByIdAsync(Guid id)
        {
            try
            {
                return await _context.books
                    .Include(x => x.Author)
                    .Include(x => x.Category)
                    .Include(x => x.SubCategory)
                    .FirstOrDefaultAsync(x => x.Id == id && x.CurrentState > 0);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<Book>> GetAllAsync()
        {
            try
            {
                return await _context.books
                    .Include(x => x.Author)
                    .Include(x => x.Category)
                    .Where(x => x.CurrentState > 0)
                    .OrderBy(x => x.Name)
                    .ToListAsync();
            }
            catch (Exception)
            {
                return new List<Book>();
            }
        }

        public async Task<bool> SaveAsync(Book model)
        {
            try
            {
                var result = await FindByIdAsync(model.Id);
                if (result is null)
                {
                    model.Id = Guid.NewGuid();
                    model.CurrentState = (int)Helper.ECurrentState.Active;
                    _context.books.Add(model);
                }
                else
                {
                    result.Name = model.Name;
                    result.AuthorId = model.AuthorId;
                    result.Publisher = model.Publisher;
                    result.PublishingDate = model.PublishingDate;
                    result.ImageUrl = model.ImageUrl;
                    result.ImageThumbnailUrl = model.ImageThumbnailUrl;
                    result.ImagePublicId = model.ImagePublicId;
                    result.Hall = model.Hall;
                    result.IsAvailableForRental = model.IsAvailableForRental;
                    result.Description = model.Description;
                    result.CategoryId = model.CategoryId;
                    result.SubCategoryId = model.SubCategoryId;
                    result.CurrentState = (int)Helper.ECurrentState.Active;
                    _context.books.Update(result);
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
