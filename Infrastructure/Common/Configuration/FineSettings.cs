namespace Infrastructure.Common.Configuration
{
    public class FineSettings
    {
        public decimal DailyRate { get; set; } = 1.00m;
        public int GracePeriodDays { get; set; } = 0;
        public decimal MaxFinePerBook { get; set; } = 50.00m;
        public string Currency { get; set; } = "USD";
    }
}
