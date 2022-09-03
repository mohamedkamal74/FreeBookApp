using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.IRepository
{
    public interface IServiceRepository<T> where T : class
    {
        List<T> GetAll();
        T FindById(Guid Id);
        bool Save(T model,Guid UserId);
        bool Delete(Guid Id);
    }
}
