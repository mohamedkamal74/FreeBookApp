using Domin.Entity;
using Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.IRepository.ServiceRepository
{
    public class ServiceLogCategory : IServiceRepositoryLog<LogCategory>
    {
        private readonly FreeBookDbContext _context;

        public ServiceLogCategory(FreeBookDbContext context)
        {
            _context = context;
        }
        public bool Delete(Guid Id, Guid UserId)
        {
            try
            {
                var logCategory = new LogCategory
                {
                    Id=Guid.NewGuid(),
                    Action=Helper.Delete,
                    UserId=UserId,
                    Date=DateTime.Now,
                    CategoryId=Id
                };
                _context.Logcategories.Add(logCategory);
                _context.SaveChanges();

                return true;
            }
            catch (Exception)
            {

                return false; ;
            }
        }

        public bool DeleteLog(Guid Id)
        {
            try
            {
                var result = FindById(Id);
                if(result != null)
                {
                    _context.Logcategories.Remove(result);
                    _context.SaveChanges();
                    return true;
                }
                return false;
            }
            catch (Exception)
            {

                return false;
            }
        }

        public LogCategory FindById(Guid Id)
        {
            try
            {
                return _context.Logcategories.FirstOrDefault(x => x.Id == Id);
            }
            catch (Exception)
            {

                return null;
            }
        }

        public List<LogCategory> GetAll()
        {
            try
            {
                return _context.Logcategories.OrderByDescending(x => x.Date).ToList();
            }
            catch (Exception)
            {

                return null;
            }
        }

        public bool Save(Guid Id, Guid UserId)
        {
            try
            {
                var logCategory = new LogCategory
                {
                    Id = Guid.NewGuid(),
                    Action = Helper.Save,
                    UserId = UserId,
                    Date = DateTime.Now,
                    CategoryId = Id
                };
                _context.Logcategories.Add(logCategory);
                _context.SaveChanges();

                return true;
            }
            catch (Exception)
            {

                return false; ;
            }
        }

        public bool Update(Guid Id, Guid UserId)
        {
            try
            {
                var logCategory = new LogCategory
                {
                    Id = Guid.NewGuid(),
                    Action = Helper.Update,
                    UserId = UserId,
                    Date = DateTime.Now,
                    CategoryId = Id
                };
                _context.Logcategories.Add(logCategory);
                _context.SaveChanges();

                return true;
            }
            catch (Exception)
            {

                return false; ;
            }
        }
    }
}
