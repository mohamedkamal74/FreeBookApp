using Infrastructure.Common;
using Infrastructure.Common.Interfaces;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FreeBook.Areas.Admin.Controllers
{
    [Area(Domin.Entity.Helper.Admin)]
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly IServiceRepository<CategoryDto, CategoryCommand> _serviceCategory;
        private readonly IServiceRepositoryLog<LogCategoryDto> _serviceCategoryLog;
        private readonly UserManager<ApplicationUser> _userManager;
        private IFlashMessageService Flash { get; }

        public CategoriesController(
          IServiceRepository<CategoryDto, CategoryCommand> serviceCategory,
          IServiceRepositoryLog<LogCategoryDto> serviceCategoryLog,
          UserManager<ApplicationUser> userManager,
          IFlashMessageService flash)
        {
            _serviceCategory = serviceCategory;
            _serviceCategoryLog = serviceCategoryLog;
            _userManager = userManager;
            Flash = flash;
        }

        public async Task<IActionResult> Categories(CategoryViewModel model)
        {
            var categoriesResult = await _serviceCategory.GetAllAsync();
            var logCategoriesResult = await _serviceCategoryLog.GetAllAsync();

            return View(new CategoryViewModel
            {
                Categories = categoriesResult.IsSuccess ? categoriesResult.Value ?? new List<CategoryDto>() : new List<CategoryDto>(),
                LogCategories = logCategoriesResult.IsSuccess ? logCategoriesResult.Value ?? new List<LogCategoryDto>() : new List<LogCategoryDto>(),
                NewCategory = new NewCategory()
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
                    Flash.Error(Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgDupplicateNameCategory);
                else
                {
                    var saveResult = await _serviceCategory.SaveAsync(model.NewCategory, null);
                    var logResult = await _serviceCategoryLog.SaveAsync(model.NewCategory.Id, Guid.Parse(userId));
                    if (saveResult.IsSuccess && logResult.IsSuccess)
                        Flash.Success(Resources.ResourceWeb.lbSave, Resources.ResourceWeb.lbSaveMsgCategory);
                    else
                        Flash.Error(Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgNotSaveCategory);
                }
            }
            else
            {
                // update
                Guid? id = model.NewCategory.Id;
                var saveResult = await _serviceCategory.SaveAsync(model.NewCategory, id);
                var logResult = await _serviceCategoryLog.UpdateAsync(model.NewCategory.Id, Guid.Parse(userId));
                if (saveResult.IsSuccess && logResult.IsSuccess)
                    Flash.Success(Resources.ResourceWeb.lbUpdate, Resources.ResourceWeb.lbUpdateMsgCategory);
                else
                    Flash.Error(Resources.ResourceWeb.lbNotUpdate, Resources.ResourceWeb.lbMsgNotUpdateCategory);
            }

            return RedirectToAction(nameof(Categories));
        }
    }
}
