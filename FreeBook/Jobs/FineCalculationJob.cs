using System;
using System.Threading.Tasks;
using Domin.Entity;
using Hangfire;
using Infrastructure.Common.Configuration;
using Infrastructure.Common.Interfaces;
using Infrastructure.IRepository;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FreeBook.Jobs
{
    [Queue("fines")]
    public class FineCalculationJob
    {
        private readonly IRentalRepository _rentalRepo;
        private readonly IServiceRepository<Subscriber> _subscriberRepo;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;
        private readonly FineSettings _fineSettings;
        private readonly ILogger<FineCalculationJob> _logger;

        public FineCalculationJob(
            IRentalRepository rentalRepo,
            IServiceRepository<Subscriber> subscriberRepo,
            IEmailService emailService,
            ISmsService smsService,
            IOptions<FineSettings> fineSettings,
            ILogger<FineCalculationJob> logger)
        {
            _rentalRepo = rentalRepo;
            _subscriberRepo = subscriberRepo;
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

                var overdueRentals = await _rentalRepo.GetOverdueRentalsAsync();
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
                        await _rentalRepo.SaveAsync(rental);
                        finesApplied++;

                        var subscriber = await _subscriberRepo.FindByIdAsync(rental.SubscriberId);
                        if (subscriber != null)
                        {
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
    }
}
