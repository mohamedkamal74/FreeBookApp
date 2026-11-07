using Domin.Entity;
using Infrastructure.Common;
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
            var result = await _serviceGovernorate.GetAllAsync();
            return View(new GovernorateViewModel
            {
                Governorates = result.IsSuccess ? result.Value ?? new List<Governorate>() : new List<Governorate>(),
                NewGovernorate = new NewGovernorate()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGovernorate(Guid Id)
        {
            var result = await _serviceGovernorate.DeleteAsync(Id);
            if (result.IsSuccess)
                return RedirectToAction(nameof(Governorates));
            SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, result.Error?.Message ?? "Cannot delete governorate with associated areas or subscribers");
            return RedirectToAction(nameof(Governorates));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(GovernorateViewModel model)
        {
            if (model.NewGovernorate.GovernorateId == null || model.NewGovernorate.GovernorateId == Guid.Empty.ToString())
            {
                // create
                var findResult = await _serviceGovernorate.FindByAsync(model.NewGovernorate.Name);
                if (findResult.IsSuccess && findResult.Value != null)
                    SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbMsgDupplicateNameCategory);
                else
                {
                    var newGovernorate = new Governorate
                    {
                        Id = Guid.NewGuid(),
                        Name = model.NewGovernorate.Name
                    };
                    var saveResult = await _serviceGovernorate.SaveAsync(newGovernorate);
                    if (saveResult.IsSuccess)
                        SessionMsg(Helper.Success, Resources.ResourceWeb.lbSave, Resources.ResourceWeb.lbSaveMsgRole);
                    else
                        SessionMsg(Helper.Error, Resources.ResourceWeb.lbNotSave, Resources.ResourceWeb.lbNotSaveMsgRole);
                }
            }
            else
            {
                // update
                var existingResult = await _serviceGovernorate.FindByIdAsync(Guid.Parse(model.NewGovernorate.GovernorateId));
                if (existingResult.IsSuccess && existingResult.Value is not null)
                {
                    existingResult.Value.Name = model.NewGovernorate.Name;
                    var saveResult = await _serviceGovernorate.SaveAsync(existingResult.Value);
                    if (saveResult.IsSuccess)
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
