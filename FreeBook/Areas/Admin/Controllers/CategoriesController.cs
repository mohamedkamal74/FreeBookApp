using Domin.Entity;
using Infrastructure.IRepository;
using Microsoft.AspNetCore.Mvc;

namespace FreeBook.Areas.Admin.Controllers
{
    [Area(Helper.Admin)]
    public class CategoriesController : Controller
    {
        private readonly IServiceRepository<Category> _serviceCategory;

        public CategoriesController(IServiceRepository<Category> serviceCategory)
        {
           _serviceCategory = serviceCategory;
        }
        public IActionResult Index()
        {
            return View(_serviceCategory.GetAll());
        }
    }
}
