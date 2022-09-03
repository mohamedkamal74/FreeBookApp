using Domin.Entity;
using Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.IRepository.ServiceRepository
{
    public class ServicesCategory : IServiceRepository<Category>
    {
        private readonly FreeBookDbContext _context;

        public ServicesCategory(FreeBookDbContext context)
        {
            _context = context;
        }
        public bool Delete(Guid Id)
        {
            throw new NotImplementedException();
        }

        public Category FindById(Guid Id)
        {
            throw new NotImplementedException();
        }

        public List<Category> GetAll()
        {
            try
            {
                return _context.categories.OrderBy(x => x.Name).ToList(); 
            }
            catch (Exception)
            {

                return null;
            }
        }

        public bool Save(Category model, Guid UserId)
        {
            throw new NotImplementedException();
        }
    }
}
