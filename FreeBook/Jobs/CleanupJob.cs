using System;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;

namespace FreeBook.Jobs
{
    [Queue("default")]
    public class CleanupJob
    {
        private readonly ILogger<CleanupJob> _logger;

        public CleanupJob(ILogger<CleanupJob> logger)
        {
            _logger = logger;
        }

        public async Task CleanupAsync()
        {
            try
            {
                _logger.LogInformation("Starting cleanup job");

                // Clean up old succeeded/failed jobs (older than 30 days)
                // Using BackgroundJob.Delete with a simple approach
                var cutoffDate = DateTime.UtcNow.AddDays(-30);

                // Note: This is a simplified cleanup. In production, you might want to use
                // Hangfire's built-in cleanup or a more sophisticated approach.
                // For now, we'll just log that cleanup ran.

                _logger.LogInformation("Cleanup job completed. (Simplified cleanup - implement detailed cleanup based on Hangfire version)");
            }
            catch (Exception ex)
            {
                // Log but don't throw - cleanup failures shouldn't break the app
                _logger.LogError(ex, "Error in cleanup job");
            }
        }
    }
}
