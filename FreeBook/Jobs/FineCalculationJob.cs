using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Infrastructure.Common.Configuration;
using Infrastructure.Common.Interfaces;
using Infrastructure.IRepository;
using Infrastructure.ViewModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FreeBook.Jobs
{
    [Queue("fines")]
    public class FineCalculationJob
    {
        private readonly IRentalRepository _rentalRepo;
        private readonly IServiceRepository<SubscriberDto, SubscriberCommand> _subscriberRepo;
        private readonly INotificationRepository _notificationRepo;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;
        private readonly FineSettings _fineSettings;
        private readonly ILogger<FineCalculationJob> _logger;

        public FineCalculationJob(
            IRentalRepository rentalRepo,
            IServiceRepository<SubscriberDto, SubscriberCommand> subscriberRepo,
            INotificationRepository notificationRepo,
            IEmailService emailService,
            ISmsService smsService,
            IOptions<FineSettings> fineSettings,
            ILogger<FineCalculationJob> logger)
        {
            _rentalRepo = rentalRepo;
            _subscriberRepo = subscriberRepo;
            _notificationRepo = notificationRepo;
            _emailService = emailService;
            _smsService = smsService;
            _fineSettings = fineSettings.Value;
            _logger = logger;
        }

        public async Task CalculateDailyFinesAsync()
        {
            try
            {
                _logger.LogInformation("Starting daily fine calculation job");

                var overdueResult = await _rentalRepo.GetOverdueRentalsAsync();
                if (!overdueResult.IsSuccess)
                {
                    _logger.LogError("Failed to get overdue rentals: {Error}", overdueResult.Error?.Message);
                    return;
                }

                var overdueRentals = overdueResult.Value ?? new List<RentalDto>();
                var now = DateTime.UtcNow;
                int finesApplied = 0;

                foreach (var rental in overdueRentals)
                {
                    foreach (var copy in rental.RentalCopies.Where(c => c.ReturnDate == null && c.EndDate < now))
                    {
                        var daysOverdue = (now - copy.EndDate).Days - _fineSettings.GracePeriodDays;
                        if (daysOverdue <= 0) continue;

                        var fine = daysOverdue * _fineSettings.DailyRate;
                        if (fine > _fineSettings.MaxFinePerBook) fine = _fineSettings.MaxFinePerBook;

                        rental.TotalFine = (rental.TotalFine ?? 0) + fine;
                        var command = ToCommand(rental);
                        command.TotalFine = rental.TotalFine;
                        var saveResult = await _rentalRepo.SaveAsync(command, rental.Id);
                        if (!saveResult.IsSuccess)
                        {
                            _logger.LogWarning("Failed to save rental {RentalId}: {Error}", rental.Id, saveResult.Error?.Message);
                            continue;
                        }
                        finesApplied++;

                        var subscriberResult = await _subscriberRepo.FindByIdAsync(rental.SubscriberId);
                        if (subscriberResult.IsSuccess && subscriberResult.Value != null)
                        {
                            var subscriber = subscriberResult.Value;
                            var bookTitle = copy.BookCopy?.Book?.Name;

                            var saved = await _notificationRepo.UpsertAsync(new NotificationCommand
                            {
                                Type = NotificationKindDto.FineApplied,
                                DedupeKey = $"fine:{rental.Id}:{copy.BookCopyId}",
                                SubscriberName = subscriber.FullName,
                                BookTitle = bookTitle,
                                Days = daysOverdue,
                                Amount = fine,
                                TargetUrl = "/Admin/Rentals/Rentals"
                            });
                            if (!saved.IsSuccess)
                                _logger.LogWarning("Failed to save fine notification: {Error}", saved.Error?.Message);

                            if (subscriber.HasWhatsApp && !string.IsNullOrEmpty(subscriber.MobileNumber))
                            {
                                await _smsService.SendAsync(subscriber.MobileNumber,
                                    $"Bookify: غرامة {fine} على كتاب متأخر. يرجى الدفع.");
                            }
                        }
                    }
                }

                _logger.LogInformation("Fine calculation job completed. Fines applied: {Count}", finesApplied);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in fine calculation job");
                throw;
            }
        }

        private static RentalCommand ToCommand(RentalDto rental)
        {
            var command = new RentalCommand
            {
                SubscriberId = rental.SubscriberId,
                StartDate = rental.StartDate,
                PenaltyPaid = rental.PenaltyPaid,
                TotalFine = rental.TotalFine
            };

            foreach (var copy in rental.RentalCopies)
            {
                command.RentalCopies.Add(new RentalCopyCommand
                {
                    BookCopyId = copy.BookCopyId,
                    RentalDate = copy.RentalDate,
                    EndDate = copy.EndDate,
                    ReturnDate = copy.ReturnDate,
                    ExtendedOn = copy.ExtendedOn
                });
            }

            return command;
        }
    }
}
