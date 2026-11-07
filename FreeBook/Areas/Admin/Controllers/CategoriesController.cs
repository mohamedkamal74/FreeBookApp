using Domin.Entity;
using Infrastructure.Common;
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
            var categoriesResult = await _serviceCategory.GetAllAsync();
            var logCategoriesResult = await _serviceCategoryLog.GetAllAsync();

            return View(new CategoryViewModel
            {
                Categories = categoriesResult.IsSuccess ? categoriesResult.Value ?? new List<Category>() : new List<Category>(),
                LogCategories = logCategoriesResult.IsSuccess ? logCategoriesResult.Value ?? new List<LogCategory>() : new List<LogCategory>(),
                NewCategory = new Category()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid Id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId is not null)
            {
                var deleteResult = await _serviceCategory.DeleteAsync(Id);
                var logResult = await _serviceCategoryLog.DeleteAsync(Id, Guid.Parse(userId));
                if (deleteResult.IsSuccess && logResult.IsSuccess)
                    return RedirectToAction(nameof(Categories));
            }
            return RedirectToAction(nameof(Categories));

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLog(Guid Id)
        {

            var result = await _serviceCategoryLog.DeleteLogAsync(Id);
            if (result.IsSuccess)
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
                var findResult = await _serviceCategory.FindByAsync(model.NewCategory.Name);
                if (findResult.IsSuccess && findResult.Value != null)
                    SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgDupplicateNameCategory);
                else
                {
                    var saveResult = await _serviceCategory.SaveAsync(model.NewCategory);
                    var logResult = await _serviceCategoryLog.SaveAsync(model.NewCategory.Id, Guid.Parse(userId));
                    if (saveResult.IsSuccess && logResult.IsSuccess)
                        SessionMsg(Helper.Success, "تم الحفظ بنجاح", "تم حفظ  الفئة بنجاح");
                    else
                        SessionMsg(Helper.Error, "حدث خطا في الحفظ", "لم يتم حفظ اسم الفئة");
                }

            }
            else
            {
                // update
                var saveResult = await _serviceCategory.SaveAsync(model.NewCategory);
                var logResult = await _serviceCategoryLog.UpdateAsync(model.NewCategory.Id, Guid.Parse(userId));
                if (saveResult.IsSuccess && logResult.IsSuccess)
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
