using System;
using System.Linq;
using System.Threading.Tasks;
using Domin.Entity;
using Hangfire;
using Infrastructure.Common.Interfaces;
using Infrastructure.IRepository;
using Microsoft.Extensions.Logging;

namespace FreeBook.Jobs
{
    [Queue("notifications")]
    public class OverdueNotificationJob
    {
        private readonly IRentalRepository _rentalRepo;
        private readonly IServiceRepository<Subscriber> _subscriberRepo;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;
        private readonly ILogger<OverdueNotificationJob> _logger;

        public OverdueNotificationJob(
            IRentalRepository rentalRepo,
            IServiceRepository<Subscriber> subscriberRepo,
            IEmailService emailService,
            ISmsService smsService,
            ILogger<OverdueNotificationJob> logger)
        {
            _rentalRepo = rentalRepo;
            _subscriberRepo = subscriberRepo;
            _emailService = emailService;
            _smsService = smsService;
            _logger = logger;
        }

        public async Task SendOverdueNotificationsAsync()
        {
            try
            {
                _logger.LogInformation("Starting overdue notification job");

                var overdue = await _rentalRepo.GetOverdueRentalsAsync();
                int notificationsSent = 0;

                foreach (var rental in overdue)
                {
                    foreach (var copy in rental.RentalCopies.Where(c => c.ReturnDate == null && c.EndDate < DateTime.UtcNow))
                    {
                        var daysOverdue = (DateTime.UtcNow - copy.EndDate).Days;
                        var subscriber = await _subscriberRepo.FindByIdAsync(rental.SubscriberId);
                        if (subscriber == null) continue;

                        _logger.LogInformation("Overdue notification for {Subscriber}: {BookTitle} ({Days} days overdue)",
                            subscriber.FullName, copy.BookCopy.Book.Name, daysOverdue);
                        notificationsSent++;
                    }
                }

                _logger.LogInformation("Overdue notification job completed. Notifications: {Count}", notificationsSent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in overdue notification job");
                throw;
            }
        }
    }
}
