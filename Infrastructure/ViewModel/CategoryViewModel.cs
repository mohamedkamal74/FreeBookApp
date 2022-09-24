using Domin.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.ViewModel
{
    public class CategoryViewModel
    {
        public List<Category> Categories { get; set; } = new List<Category>();
        public List<LogCategory> LogCategories { get; set; } = new List<LogCategory>();
        public Category NewCategory { get; set; } = new Category();
    }
}
