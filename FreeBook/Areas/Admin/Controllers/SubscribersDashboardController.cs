using Domin.Entity;
using Infrastructure.Common;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeBook.Areas.Admin.Controllers
{
    [Area(Helper.Admin)]
    [Authorize(Roles = "Admin,User")]
    public class SubscribersDashboardController : Controller
    {
        private readonly IDashboardRepository _dashboardRepository;

        public SubscribersDashboardController(IDashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository;
        }

        public async Task<IActionResult> Index(DateTime? from = null, DateTime? to = null, Guid? governorateId = null)
        {
            var result = await _dashboardRepository.GetSubscribersAsync(new SubscribersDashboardQuery
            {
                From = from,
                To = to,
                GovernorateId = governorateId
            });

            return View(result.ValueOr(new SubscribersDashboardData()));
        }
    }
}
