using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domin.Entity;
using Hangfire;
using Infrastructure.Common;
using Infrastructure.Common.Interfaces;
using Infrastructure.IRepository;
using Microsoft.Extensions.Logging;

namespace FreeBook.Jobs
{
    [Queue("notifications")]
    public class ExpiryCheckJob
    {
        private readonly ISubscriptionRepository _subRepo;
        private readonly IServiceRepository<Subscriber> _subscriberRepo;
        private readonly INotificationRepository _notificationRepo;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;
        private readonly ILogger<ExpiryCheckJob> _logger;

        public ExpiryCheckJob(
            ISubscriptionRepository subRepo,
            IServiceRepository<Subscriber> subscriberRepo,
            INotificationRepository notificationRepo,
            IEmailService emailService,
            ISmsService smsService,
            ILogger<ExpiryCheckJob> logger)
        {
            _subRepo = subRepo;
            _subscriberRepo = subscriberRepo;
            _notificationRepo = notificationRepo;
            _emailService = emailService;
            _smsService = smsService;
            _logger = logger;
        }

        public async Task CheckExpiringSubscriptionsAsync()
        {
            try
            {
                _logger.LogInformation("Starting expiry check job");

                // One query is enough; the 7/3/1 day windows below are stages
                // of the same case, not three separate notifications.
                var expiringResult = await _subRepo.GetExpiringSubscriptionsAsync(7);
                if (!expiringResult.IsSuccess)
                {
                    _logger.LogError("Failed to get expiring subscriptions: {Error}", expiringResult.Error?.Message);
                    return;
                }

                var now = DateTime.UtcNow;
                int notificationsSent = 0;

                foreach (var sub in expiringResult.Value ?? new List<Subscription>())
                {
                    var daysLeft = Math.Max(0, (sub.EndDate.Date - now.Date).Days);
                    if (daysLeft > 7) continue;

                    var stage = daysLeft <= 1 ? 1 : daysLeft <= 3 ? 3 : 7;

                    var subscriberResult = await _subscriberRepo.FindByIdAsync(sub.SubscriberId);
                    if (!subscriberResult.IsSuccess || subscriberResult.Value == null) continue;

                    var subscriber = subscriberResult.Value;
                    _logger.LogInformation("Subscription expiring for {Subscriber} in {Days} days", subscriber.FullName, daysLeft);

                    var saved = await _notificationRepo.UpsertAsync(new Notification
                    {
                        Type = NotificationType.SubscriptionExpiring,
                        DedupeKey = $"sub:{sub.Id}:{stage}",
                        SubscriberName = subscriber.FullName,
                        Days = stage,
                        TargetUrl = "/Admin/Subscriptions/Subscriptions"
                    });
                    if (!saved.IsSuccess)
                    {
                        _logger.LogWarning("Failed to save expiry notification: {Error}", saved.Error?.Message);
                        continue;
                    }
                    notificationsSent++;
                }

                _logger.LogInformation("Expiry check job completed. Notifications: {Count}", notificationsSent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in expiry check job");
                throw;
            }
        }
    }
}
