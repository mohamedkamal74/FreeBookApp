using Domin.Entity;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FreeBook.Areas.Admin.Controllers
{
    [Area(Helper.Admin)]
    [Authorize(Roles = "Admin,User")]
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
        public async Task<IActionResult> Categories(CategoryViewModel model)
        {
            return View(new CategoryViewModel
            {
                Categories = await _serviceCategory.GetAllAsync(),
                LogCategories = await _serviceCategoryLog.GetAllAsync(),
                NewCategory = new Category()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid Id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId is not null
                && await _serviceCategory.DeleteAsync(Id)
                && await _serviceCategoryLog.DeleteAsync(Id, Guid.Parse(userId)))
                return RedirectToAction(nameof(Categories));
            return RedirectToAction(nameof(Categories));

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLog(Guid Id)
        {

            if (await _serviceCategoryLog.DeleteLogAsync(Id))
                return RedirectToAction(nameof(Categories));
            return RedirectToAction(nameof(Categories));

        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(CategoryViewModel model)
        {

            var userId = _userManager.GetUserId(User);
            if (userId is null)
                return RedirectToAction(nameof(Categories));

            if (model.NewCategory.Id == Guid.Empty)
            {
                // create
                if (await _serviceCategory.FindByAsync(model.NewCategory.Name) != null)
                    SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgDupplicateNameCategory);
                else
                {
                    if (await _serviceCategory.SaveAsync(model.NewCategory)
                       && await _serviceCategoryLog.SaveAsync(model.NewCategory.Id, Guid.Parse(userId)))
                        SessionMsg(Helper.Success, "تم الحفظ بنجاح", "تم حفظ  الفئة بنجاح");
                    else
                        SessionMsg(Helper.Error, "حدث خطا في الحفظ", "لم يتم حفظ اسم الفئة");
                }

            }
            else
            {
                // update
                if (await _serviceCategory.SaveAsync(model.NewCategory)
                      && await _serviceCategoryLog.UpdateAsync(model.NewCategory.Id, Guid.Parse(userId)))
                    SessionMsg(Helper.Success, "تم التعديل بنجاح", "تم تعديل  الفئة بنجاح");
                else
                    SessionMsg(Helper.Error, "حدث خطا في التعديل", "لم يتم تعديل اسم الفئة");
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
