using Infrastructure.ViewModel;

namespace Infrastructure.IRepository.ServiceRepository
{
    internal static class ReportQueryHelpers
    {
        internal const int DefaultChartMonths = 6;
        internal const int MaxChartMonths = 12;

        internal sealed class MonthCount
        {
            public int Year { get; set; }
            public int Month { get; set; }
            public int Count { get; set; }
        }

        internal static (DateTime From, DateTime To) NormalizeRange(DateTime? from, DateTime? to)
        {
            var today = DateTime.UtcNow.Date;
            var defaultFrom = new DateTime(today.Year, today.Month, 1).AddMonths(-(DefaultChartMonths - 1));
            var rangeFrom = (from ?? defaultFrom).Date;
            var rangeTo = (to ?? today).Date;
            return rangeFrom > rangeTo ? (rangeTo, rangeFrom) : (rangeFrom, rangeTo);
        }

        internal static List<NamedCount> MonthlyBuckets(IEnumerable<MonthCount> counts, DateTime from, DateTime to)
        {
            var lookup = counts.ToDictionary(count => (count.Year, count.Month), count => count.Count);
            var firstMonth = new DateTime(from.Year, from.Month, 1);
            var lastMonth = new DateTime(to.Year, to.Month, 1);
            var monthCount = (lastMonth.Year - firstMonth.Year) * 12 + lastMonth.Month - firstMonth.Month + 1;
            if (monthCount > MaxChartMonths)
            {
                monthCount = MaxChartMonths;
                firstMonth = lastMonth.AddMonths(-(MaxChartMonths - 1));
            }

            return Enumerable.Range(0, monthCount)
                .Select(i =>
                {
                    var month = firstMonth.AddMonths(i);
                    return new NamedCount
                    {
                        Name = month.ToString("MMM yyyy"),
                        Count = lookup.TryGetValue((month.Year, month.Month), out var count) ? count : 0
                    };
                })
                .ToList();
        }

        internal static int AgeOn(DateTime dateOfBirth, DateTime now)
        {
            var age = now.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > now.Date.AddYears(-age))
            {
                age--;
            }

            return Math.Max(age, 0);
        }
    }
}
