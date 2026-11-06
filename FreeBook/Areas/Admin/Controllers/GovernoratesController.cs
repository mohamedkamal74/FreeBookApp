using Domin.Entity;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeBook.Areas.Admin.Controllers
{
    [Area(Helper.Admin)]
    [Authorize(Roles = "Admin")]
    public class GovernoratesController : Controller
    {
        private readonly IServiceRepository<Governorate> _serviceGovernorate;

        public GovernoratesController(IServiceRepository<Governorate> serviceGovernorate)
        {
            _serviceGovernorate = serviceGovernorate;
        }

        public async Task<IActionResult> Governorates(GovernorateViewModel model)
        {
            return View(new GovernorateViewModel
            {
                Governorates = await _serviceGovernorate.GetAllAsync(),
                NewGovernorate = new NewGovernorate()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGovernorate(Guid Id)
        {
            if (await _serviceGovernorate.DeleteAsync(Id))
                return RedirectToAction(nameof(Governorates));
            SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, "Cannot delete governorate with associated areas or subscribers");
            return RedirectToAction(nameof(Governorates));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(GovernorateViewModel model)
        {
            if (model.NewGovernorate.GovernorateId == null || model.NewGovernorate.GovernorateId == Guid.Empty.ToString())
            {
                // create
                if (await _serviceGovernorate.FindByAsync(model.NewGovernorate.Name) != null)
                    SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgDupplicateNameCategory);
                else
                {
                    var newGovernorate = new Governorate
                    {
                        Id = Guid.NewGuid(),
                        Name = model.NewGovernorate.Name
                    };
                    if (await _serviceGovernorate.SaveAsync(newGovernorate))
                        SessionMsg(Helper.Success, Resources.ResourceWeb.lbSave, Resources.ResourceWeb.lbSaveMsgRole);
                    else
                        SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbNotSaveMsgRole);
                }
            }
            else
            {
                // update
                var existingGovernorate = await _serviceGovernorate.FindByIdAsync(Guid.Parse(model.NewGovernorate.GovernorateId));
                if (existingGovernorate is not null)
                {
                    existingGovernorate.Name = model.NewGovernorate.Name;
                    if (await _serviceGovernorate.SaveAsync(existingGovernorate))
                        SessionMsg(Helper.Success, Resources.ResourceWeb.lbUpdate, Resources.ResourceWeb.lbUpdateMsgRole);
                    else
                        SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotUpdate, Resources.ResourceWeb.lbNotUpdateMsgRole);
                }
            }

            return RedirectToAction(nameof(Governorates));
        }

        private void SessionMsg(string MsgType, string Title, string Msg)
        {
            HttpContext.Session.SetString(Helper.MsgType, MsgType);
            HttpContext.Session.SetString(Helper.Title, Title);
            HttpContext.Session.SetString(Helper.Msg, Msg);
        }
    }
}
