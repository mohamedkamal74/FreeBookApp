using Domin.Entity;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FreeBook.Areas.Admin.Controllers
{
    [Area(Helper.Admin)]
    public class CategoriesController : Controller
    {
        private readonly IServiceRepository<Category> _serviceCategory;
        private readonly IServiceRepositoryLog<LogCategory> _serviceCategoryLog;
        private readonly UserManager<ApplicationUser> _userManager;

        public CategoriesController(IServiceRepository<Category> serviceCategory,
          IServiceRepositoryLog<LogCategory> serviceCategoryLog,
          UserManager<ApplicationUser> userManager)
        {
            _serviceCategory = serviceCategory;
            _serviceCategoryLog = serviceCategoryLog;
            _userManager = userManager;
        }
        public IActionResult Categories(CategoryViewModel model)
        {
            return View(new CategoryViewModel
            {
                Categories = _serviceCategory.GetAll(),
                LogCategories = _serviceCategoryLog.GetAll(),
                NewCategory = new Category()
            });
        }

        public IActionResult Delete(Guid Id)
        {
            var userId = _userManager.GetUserId(User);
            if (_serviceCategory.Delete(Id) && _serviceCategoryLog.Delete(Id, Guid.Parse(userId)))
                return RedirectToAction(nameof(Categories));
            return RedirectToAction(nameof(Categories));

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save(CategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);

                if (model.NewCategory.Id == Guid.Parse(Guid.Empty.ToString()))
                {
                    // create
                    if (_serviceCategory.FindBy(model.NewCategory.Name) != null) 
                        SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgDupplicateNameCategory);
                    else
                    {
                        if (_serviceCategory.Save(model.NewCategory)
                           && _serviceCategoryLog.Save(model.NewCategory.Id, Guid.Parse(userId)))
                        {
                           
                            SessionMsg(Helper.Success, "تم الحفظ بنجاح", "تم حفظ  الفئة بنجاح" );
                           
                        }
                        else
                            SessionMsg(Helper.Error, "حدث خطا في الحفظ", "لم يتم حفظ اسم الفئة");
                    }

                }
                else
                {
                    // update

                    if (_serviceCategory.Save(model.NewCategory) 
                          && _serviceCategoryLog.Update(model.NewCategory.Id, Guid.Parse(userId)))
                        SessionMsg(Helper.Success, "تم التعديل بنجاح", "تم تعديل  الفئة بنجاح");
                    else
                        SessionMsg(Helper.Error, "حدث خطا في التعديل", "لم يتم تعديل اسم الفئة");
                }
            }
            return RedirectToAction(nameof(Categories));
        }

        private void SessionMsg(string MsgType, string Title, string Msg)
        {
            HttpContext.Session.SetString(Helper.MsgType, MsgType);
            HttpContext.Session.SetString(Helper.Title, Title);
            HttpContext.Session.SetString(Helper.Msg, Msg);
        }
    }
}
