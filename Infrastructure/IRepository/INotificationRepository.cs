using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure.Common;
using Infrastructure.ViewModel;

namespace Infrastructure.IRepository
{
    public interface INotificationRepository
    {
        Task<Result<List<NotificationDto>>> GetRecentAsync(int take = 50);

        Task<Result<int>> GetUnreadCountAsync();

        /// <summary>
        /// Inserts the notification or refreshes the mutable fields of the row
        /// with the same dedupe key. Returns true when a new row was inserted.
        /// </summary>
        Task<Result<bool>> UpsertAsync(NotificationCommand command);

        Task<Result<bool>> MarkReadAsync(Guid id);

        Task<Result<int>> MarkAllReadAsync();

        Task<Result<int>> DeleteReadAsync();
    }
}
