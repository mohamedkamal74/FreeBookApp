using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domin.Entity;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.IRepository.ServiceRepository
{
    public class ServicesNotification : INotificationRepository
    {
        private readonly FreeBookDbContext _context;
        private readonly ILogger<ServicesNotification> _logger;

        public ServicesNotification(FreeBookDbContext context, ILogger<ServicesNotification> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Result<List<NotificationDto>>> GetRecentAsync(int take = 50)
        {
            try
            {
                var notifications = await _context.Notifications
                    .AsNoTracking()
                    .OrderBy(x => x.IsRead)
                    .ThenByDescending(x => x.CreatedOn)
                    .Take(take)
                    .ToListAsync();
                return Result.Success(notifications.Select(CrudMapping.ToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent notifications");
                return Result.Failure<List<NotificationDto>>("GET_NOTIFICATIONS_ERROR", "Failed to get notifications", ex.Message);
            }
        }

        public async Task<Result<int>> GetUnreadCountAsync()
        {
            try
            {
                var count = await _context.Notifications.CountAsync(x => !x.IsRead);
                return Result.Success(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting unread notifications");
                return Result.Failure<int>("COUNT_NOTIFICATIONS_ERROR", "Failed to count unread notifications", ex.Message);
            }
        }

        public async Task<Result<bool>> UpsertAsync(NotificationCommand command)
        {
            try
            {
                var existing = await _context.Notifications
                    .SingleOrDefaultAsync(x => x.DedupeKey == command.DedupeKey);

                if (existing is null)
                {
                    _context.Notifications.Add(new Notification
                    {
                        Id = Guid.NewGuid(),
                        Type = CrudMapping.ToEntity(command.Type),
                        DedupeKey = command.DedupeKey,
                        SubscriberName = command.SubscriberName,
                        BookTitle = command.BookTitle,
                        Days = command.Days,
                        Amount = command.Amount,
                        TargetUrl = command.TargetUrl,
                        CreatedOn = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                    return Result.Success(true);
                }

                // Keep the original CreatedOn and read state; only refresh the
                // mutable event details so a re-run never floods the list.
                existing.Type = CrudMapping.ToEntity(command.Type);
                existing.SubscriberName = command.SubscriberName;
                existing.BookTitle = command.BookTitle;
                existing.Days = command.Days;
                existing.Amount = command.Amount;
                existing.TargetUrl = command.TargetUrl;
                await _context.SaveChangesAsync();
                return Result.Success(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving notification {Key}", command.DedupeKey);
                return Result.Failure<bool>("SAVE_NOTIFICATION_ERROR", "Failed to save notification", ex.Message);
            }
        }

        public async Task<Result<bool>> MarkReadAsync(Guid id)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(id);
                if (notification is null)
                    return Result.Failure<bool>("NOT_FOUND", $"Notification with id {id} not found");

                if (!notification.IsRead)
                {
                    notification.IsRead = true;
                    notification.ReadOn = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {Id} as read", id);
                return Result.Failure<bool>("READ_NOTIFICATION_ERROR", "Failed to mark notification as read", ex.Message);
            }
        }

        public async Task<Result<int>> MarkAllReadAsync()
        {
            try
            {
                var unread = await _context.Notifications.Where(x => !x.IsRead).ToListAsync();
                if (unread.Count == 0)
                    return Result.Success(0);

                var now = DateTime.UtcNow;
                foreach (var notification in unread)
                {
                    notification.IsRead = true;
                    notification.ReadOn = now;
                }
                await _context.SaveChangesAsync();
                return Result.Success(unread.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return Result.Failure<int>("READ_NOTIFICATIONS_ERROR", "Failed to mark notifications as read", ex.Message);
            }
        }

        public async Task<Result<int>> DeleteReadAsync()
        {
            try
            {
                var read = await _context.Notifications.Where(x => x.IsRead).ToListAsync();
                if (read.Count == 0)
                    return Result.Success(0);

                _context.Notifications.RemoveRange(read);
                await _context.SaveChangesAsync();
                return Result.Success(read.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting read notifications");
                return Result.Failure<int>("DELETE_NOTIFICATIONS_ERROR", "Failed to delete read notifications", ex.Message);
            }
        }
    }
}
