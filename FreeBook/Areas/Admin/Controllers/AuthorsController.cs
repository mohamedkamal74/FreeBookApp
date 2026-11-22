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
    public class AuthorsController : Controller
    {
        private readonly IAuthorRepository _serviceAuthor;
        private readonly UserManager<ApplicationUser> _userManager;
        private IFlashMessageService Flash { get; }

        public AuthorsController(
            IAuthorRepository serviceAuthor,
            UserManager<ApplicationUser> userManager,
            IFlashMessageService flash)
        {
            _serviceAuthor = serviceAuthor;
            _userManager = userManager;
            Flash = flash;
        }

        public async Task<IActionResult> Authors(AuthorViewModel model)
        {
            var authorsResult = await _serviceAuthor.GetAllActiveAsync();
            return View(new AuthorViewModel
            {
                Authors = authorsResult.IsSuccess ? authorsResult.Value ?? new List<AuthorDto>() : new List<AuthorDto>(),
                NewAuthor = new NewAuthor()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAuthor(Guid Id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId is not null)
            {
                var result = await _serviceAuthor.DeleteAsync(Id);
                if (result.IsSuccess)
                    return RedirectToAction(nameof(Authors));
            }
            return RedirectToAction(nameof(Authors));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(AuthorViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            if (userId is null)
                return RedirectToAction(nameof(Authors));

            if (model.NewAuthor.AuthorId == null || model.NewAuthor.AuthorId == Guid.Empty.ToString())
            {
                // create
                var findResult = await _serviceAuthor.FindByNameAsync(model.NewAuthor.Name);
                if (findResult.IsSuccess && findResult.Value != null)
                    Flash.Error(Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgDuplicateName);
                else
                {
                    var command = new AuthorCommand { Name = model.NewAuthor.Name };
                    var saveResult = await _serviceAuthor.SaveAsync(command);
                    if (saveResult.IsSuccess)
                        Flash.Success(Resources.ResourceWeb.lbSave, Resources.ResourceWeb.lbMsgSavedOk);
                    else
                        Flash.Error(Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgNotSavedOk);
                }
            }
            else
            {
                // update
                var existingAuthorResult = await _serviceAuthor.FindByIdAsync(Guid.Parse(model.NewAuthor.AuthorId));
                if (existingAuthorResult.IsSuccess && existingAuthorResult.Value is not null)
                {
                    // Check for duplicate name (excluding self)
                    var findResult = await _serviceAuthor.FindByNameAsync(model.NewAuthor.Name);
                    if (findResult.IsSuccess && findResult.Value != null && findResult.Value.Id != existingAuthorResult.Value.Id)
                    {
                        Flash.Error(Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgDuplicateName);
                    }
                    else
                    {
                        var command = new AuthorCommand { Name = model.NewAuthor.Name };
                        var saveResult = await _serviceAuthor.SaveAsync(command, existingAuthorResult.Value.Id);
                        if (saveResult.IsSuccess)
                            Flash.Success(Resources.ResourceWeb.lbUpdate, Resources.ResourceWeb.lbMsgUpdatedOk);
                        else
                            Flash.Error(Resources.ResourceWeb.lbNotUpdate, Resources.ResourceWeb.lbMsgNotUpdatedOk);
                    }
                }
            }

            return RedirectToAction(nameof(Authors));
        }
    }
}
