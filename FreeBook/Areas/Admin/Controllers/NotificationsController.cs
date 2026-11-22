using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure.Common;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeBook.Areas.Admin.Controllers
{
    [Area(Domin.Entity.Helper.Admin)]
    [Authorize(Roles = "Admin")]
    public class NotificationsController : Controller
    {
        private readonly INotificationRepository _notifications;

        public NotificationsController(INotificationRepository notifications)
        {
            _notifications = notifications;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var result = await _notifications.GetRecentAsync(100);
            if (!result.IsSuccess)
            {
                TempData["NotificationError"] = result.Error?.Message;
                return View(new List<NotificationDto>());
            }
            return View(result.Value ?? new List<NotificationDto>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(Guid id)
        {
            await _notifications.MarkReadAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            await _notifications.MarkAllReadAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRead()
        {
            await _notifications.DeleteReadAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
