using Domin.Entity;
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
            return View(new AuthorViewModel
            {
                Authors = await _serviceAuthor.GetAllActiveAsync(),
                NewAuthor = new NewAuthor()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAuthor(Guid Id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId is not null && await _serviceAuthor.DeleteAsync(Id))
                return RedirectToAction(nameof(Authors));
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
                if (await _serviceAuthor.FindByNameAsync(model.NewAuthor.Name) != null)
                    SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgDupplicateNameCategory);
                else
                {
                    var newAuthor = new Author
                    {
                        Id = Guid.NewGuid(),
                        Name = model.NewAuthor.Name
                    };
                    if (await _serviceAuthor.SaveAsync(newAuthor))
                        SessionMsg(Helper.Success, Resources.ResourceWeb.lbSave, Resources.ResourceWeb.lbSaveMsgRole);
                    else
                        SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbNotSaveMsgRole);
                }
            }
            else
            {
                // update
                var existingAuthor = await _serviceAuthor.FindByIdAsync(Guid.Parse(model.NewAuthor.AuthorId));
                if (existingAuthor is not null)
                {
                    existingAuthor.Name = model.NewAuthor.Name;
                    if (await _serviceAuthor.SaveAsync(existingAuthor))
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
