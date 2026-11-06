using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domin.Entity;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IRepository.ServiceRepository
{
    public class ServicesBookCopy : IServiceRepository<BookCopy>
    {
        private readonly FreeBookDbContext _context;

        public ServicesBookCopy(FreeBookDbContext context)
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

                _context.BookCopies.Remove(result);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<BookCopy?> FindByAsync(string name)
        {
            try
            {
                var trimmed = name.Trim();
                return await _context.BookCopies
                    .Include(x => x.Book)
                    .FirstOrDefaultAsync(x => x.Book != null && x.Book.Name.Equals(trimmed));
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<BookCopy?> FindByIdAsync(Guid id)
        {
            try
            {
                return await _context.BookCopies
                    .Include(x => x.Book)
                    .FirstOrDefaultAsync(x => x.Id == id);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<BookCopy>> GetAllAsync()
        {
            try
            {
                return await _context.BookCopies
                    .Include(x => x.Book)
                    .OrderBy(x => x.Book != null ? x.Book.Name : "")
                    .ToListAsync();
            }
            catch (Exception)
            {
                return new List<BookCopy>();
            }
        }

        public async Task<bool> SaveAsync(BookCopy model)
        {
            try
            {
                var result = await FindByIdAsync(model.Id);
                if (result is null)
                {
                    model.Id = Guid.NewGuid();
                    _context.BookCopies.Add(model);
                }
                else
                {
                    result.BookId = model.BookId;
                    result.EditionNumber = model.EditionNumber;
                    result.SerialNumber = model.SerialNumber;
                    result.IsAvailableForRental = model.IsAvailableForRental;
                    _context.BookCopies.Update(result);
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
