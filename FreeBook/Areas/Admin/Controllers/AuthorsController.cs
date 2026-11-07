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
    [Authorize(Roles = "Admin")]
    public class AuthorsController : Controller
    {
        private readonly IAuthorRepository _serviceAuthor;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthorsController(IAuthorRepository serviceAuthor, UserManager<ApplicationUser> userManager)
        {
            _serviceAuthor = serviceAuthor;
            _userManager = userManager;
        }

        public async Task<IActionResult> Authors(AuthorViewModel model)
        {
            var authorsResult = await _serviceAuthor.GetAllActiveAsync();
            return View(new AuthorViewModel
            {
                Authors = authorsResult.IsSuccess ? authorsResult.Value ?? new List<Author>() : new List<Author>(),
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
                    SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgDupplicateNameCategory);
                else
                {
                    var newAuthor = new Author
                    {
                        Id = Guid.NewGuid(),
                        Name = model.NewAuthor.Name
                    };
                    var saveResult = await _serviceAuthor.SaveAsync(newAuthor);
                    if (saveResult.IsSuccess)
                        SessionMsg(Helper.Success, Resources.ResourceWeb.lbSave, Resources.ResourceWeb.lbSaveMsgRole);
                    else
                        SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbNotSaveMsgRole);
                }
            }
            else
            {
                // update
                var existingAuthorResult = await _serviceAuthor.FindByIdAsync(Guid.Parse(model.NewAuthor.AuthorId));
                if (existingAuthorResult.IsSuccess && existingAuthorResult.Value is not null)
                {
                    existingAuthorResult.Value.Name = model.NewAuthor.Name;
                    var saveResult = await _serviceAuthor.SaveAsync(existingAuthorResult.Value);
                    if (saveResult.IsSuccess)
                        SessionMsg(Helper.Success, Resources.ResourceWeb.lbUpdate, Resources.ResourceWeb.lbUpdateMsgRole);
                    else
                        SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotUpdate, Resources.ResourceWeb.lbNotUpdateMsgRole);
                }
            }

            return RedirectToAction(nameof(Authors));
        }

        private void SessionMsg(string MsgType, string Title, string Msg)
        {
            HttpContext.Session.SetString(Helper.MsgType, MsgType);
            HttpContext.Session.SetString(Helper.Title, Title);
            HttpContext.Session.SetString(Helper.Msg, Msg);
        }
    }
}
