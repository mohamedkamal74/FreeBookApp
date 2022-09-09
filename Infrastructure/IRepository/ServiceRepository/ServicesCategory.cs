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
            try
            {
                var result = FindById(Id);
                result.Currentstste =(int) Helper.ECurrentState.Delete;
                _context.categories.Update(result);
                _context.SaveChanges();
                return true;
            }
            catch (Exception)
            {

                return false; ;
            }
        }

        public Category FindBy(string Name)
        {
            try
            {
                return _context.categories.FirstOrDefault(x => x.Name.Contains(Name.Trim())&&x.Currentstste>0);
            }
            catch (Exception)
            {

                return null;
            }
        }

        public Category FindById(Guid Id)
        {
            try
            {
                return _context.categories.FirstOrDefault(x => x.Id == Id&& x.Currentstste > 0);
            }
            catch (Exception)
            {

                return null;
            }
        }

        public List<Category> GetAll()
        {
            try
            {
                return _context.categories.OrderBy(x => x.Name).Where(x=>x.Currentstste>0).ToList(); 
            }
            catch (Exception)
            {

                return null;
            }
        }

        public bool Save(Category model)
        {
            try
            {
                var result=FindById(model.Id);
                if (result == null)
                {
                    if (FindBy(model.Name) != null)
                        return false;
                    model.Id=Guid.NewGuid();
                    model.Currentstste = (int)Helper.ECurrentState.Active;
                    _context.categories.Add(model);

                }
                else
                {
                    result.Name = model.Name;
                    result.Description=model.Description;
                    result.Currentstste = (int)Helper.ECurrentState.Active;
                    _context.categories.Update(result);

                }
                _context.SaveChanges();
                return true;
            }
            catch (Exception)
            {

                return false ;
            }
        }
    }
}
