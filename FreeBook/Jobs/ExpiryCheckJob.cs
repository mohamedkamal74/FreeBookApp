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
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;
        private readonly ILogger<ExpiryCheckJob> _logger;

        public ExpiryCheckJob(
            ISubscriptionRepository subRepo,
            IServiceRepository<Subscriber> subscriberRepo,
            IEmailService emailService,
            ISmsService smsService,
            ILogger<ExpiryCheckJob> logger)
        {
            _subRepo = subRepo;
            _subscriberRepo = subscriberRepo;
            _emailService = emailService;
            _smsService = smsService;
            _logger = logger;
        }

        public async Task CheckExpiringSubscriptionsAsync()
        {
            try
            {
                _logger.LogInformation("Starting expiry check job");

                var daysToCheck = new[] { 7, 3, 1 };
                int notificationsSent = 0;

                foreach (var days in daysToCheck)
                {
                    var expiringResult = await _subRepo.GetExpiringSubscriptionsAsync(days);
                    if (!expiringResult.IsSuccess)
                    {
                        _logger.LogError("Failed to get expiring subscriptions: {Error}", expiringResult.Error?.Message);
                        continue;
                    }

                    var expiring = expiringResult.Value ?? new List<Subscription>();

                    foreach (var sub in expiring)
                    {
                        var subscriberResult = await _subscriberRepo.FindByIdAsync(sub.SubscriberId);
                        if (!subscriberResult.IsSuccess || subscriberResult.Value == null) continue;

                        var subscriber = subscriberResult.Value;
                        _logger.LogInformation("Subscription expiring for {Subscriber} in {Days} days", subscriber.FullName, days);
                        notificationsSent++;
                    }
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
