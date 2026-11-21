using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domin.Entity;
using Infrastructure.Common;

namespace Infrastructure.IRepository
{
    public interface INotificationRepository
    {
        Task<Result<List<Notification>>> GetRecentAsync(int take = 50);

        Task<Result<int>> GetUnreadCountAsync();

        /// <summary>
        /// Inserts the notification or refreshes the mutable fields of the row
        /// with the same <see cref="Notification.DedupeKey"/>. Returns true when
        /// a new row was inserted, false when an existing row was updated.
        /// </summary>
        Task<Result<bool>> UpsertAsync(Notification notification);

        Task<Result<bool>> MarkReadAsync(Guid id);

        Task<Result<int>> MarkAllReadAsync();

        Task<Result<int>> DeleteReadAsync();
    }
}
