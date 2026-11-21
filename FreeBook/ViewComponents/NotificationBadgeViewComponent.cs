using System.Threading.Tasks;
using Infrastructure.IRepository;
using Microsoft.AspNetCore.Mvc;

namespace FreeBook.ViewComponents
{
    /// <summary>
    /// Renders the admin navbar bell with the current unread count. Called from
    /// the admin layout so every page shows an up-to-date badge.
    /// </summary>
    public class NotificationBadgeViewComponent : ViewComponent
    {
        private readonly INotificationRepository _notifications;

        public NotificationBadgeViewComponent(INotificationRepository notifications)
        {
            _notifications = notifications;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var result = await _notifications.GetUnreadCountAsync();
            return View(result.IsSuccess ? result.Value : 0);
        }
    }
}
