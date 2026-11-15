using Domin.Entity;
using FreeBook.Models;
using FreeBook.Resources;
using Infrastructure.Common;
using Infrastructure.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace FreeBook.Areas.Admin.Controllers
{
    [Area(Helper.Admin)]
    [Authorize(Roles = "Admin,User")]
    public class SubscribersDashboardController : Controller
    {
        private const int DefaultChartMonths = 6;
        private const int MaxChartMonths = 12;
        private const int ExpiryWarningDays = 30;

        private readonly IServiceRepository<Subscriber> _serviceSubscriber;
        private readonly IServiceRepository<Subscription> _serviceSubscription;
        private readonly IServiceRepository<Rental> _serviceRental;
        private readonly IServiceRepository<Governorate> _serviceGovernorate;
        private readonly IStringLocalizer<ResourceWeb> _localizer;

        public SubscribersDashboardController(
            IServiceRepository<Subscriber> serviceSubscriber,
            IServiceRepository<Subscription> serviceSubscription,
            IServiceRepository<Rental> serviceRental,
            IServiceRepository<Governorate> serviceGovernorate,
            IStringLocalizer<ResourceWeb> localizer)
        {
            _serviceSubscriber = serviceSubscriber;
            _serviceSubscription = serviceSubscription;
            _serviceRental = serviceRental;
            _serviceGovernorate = serviceGovernorate;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(DateTime? from = null, DateTime? to = null, Guid? governorateId = null)
        {
            var subscribersResult = await _serviceSubscriber.GetAllAsync();
            var subscriptionsResult = await _serviceSubscription.GetAllAsync();
            var rentalsResult = await _serviceRental.GetAllAsync();
            var governoratesResult = await _serviceGovernorate.GetAllAsync();

            var subscribers = subscribersResult.IsSuccess ? subscribersResult.Value ?? new List<Subscriber>() : new List<Subscriber>();
            var subscriptions = subscriptionsResult.IsSuccess ? subscriptionsResult.Value ?? new List<Subscription>() : new List<Subscription>();
            var rentals = rentalsResult.IsSuccess ? rentalsResult.Value ?? new List<Rental>() : new List<Rental>();
            var governorates = governoratesResult.IsSuccess ? governoratesResult.Value ?? new List<Governorate>() : new List<Governorate>();

            var now = DateTime.UtcNow;

            // Same range semantics as the main dashboard: default to the current month plus the five before it.
            var defaultTo = now.Date;
            var defaultFrom = new DateTime(defaultTo.Year, defaultTo.Month, 1).AddMonths(-(DefaultChartMonths - 1));
            var rangeFrom = (from ?? defaultFrom).Date;
            var rangeTo = (to ?? defaultTo).Date;
            if (rangeFrom > rangeTo)
            {
                (rangeFrom, rangeTo) = (rangeTo, rangeFrom);
            }

            var scopedSubscribers = governorateId.HasValue
                ? subscribers.Where(s => s.GovernorateId == governorateId.Value).ToList()
                : subscribers;
            var scopedIds = scopedSubscribers.Select(s => s.Id).ToHashSet();
            var scopedSubscriptions = subscriptions.Where(s => scopedIds.Contains(s.SubscriberId)).ToList();
            var scopedRentals = rentals.Where(r => scopedIds.Contains(r.SubscriberId)).ToList();

            var model = new SubscribersDashboardViewModel
            {
                TotalSubscribers = scopedSubscribers.Count,
                ActiveSubscriptions = scopedSubscriptions.Count(s => s.StartDate <= now && s.EndDate >= now),
                ExpiredSubscriptions = scopedSubscriptions.Count(s => s.EndDate < now),
                ExpiringSoon = scopedSubscriptions.Count(s => s.EndDate >= now && s.EndDate <= now.AddDays(ExpiryWarningDays)),
                Blacklisted = scopedSubscribers.Count(s => s.IsBlackListed),
                WhatsAppEnabled = scopedSubscribers.Count(s => s.HasWhatsApp)
            };

            model.ByGovernorate = scopedSubscribers
                .GroupBy(s => s.Governorate?.Name ?? "—")
                .Select(g => new NamedCount { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Name, StringComparer.Ordinal)
                .ToList();

            model.AgeGroups = BuildAgeGroups(scopedSubscribers, now);

            model.SubscriptionStatus = new List<NamedCount>
            {
                new() { Name = _localizer["lbActive"].Value, Count = model.ActiveSubscriptions },
                new() { Name = _localizer["lbExpired"].Value, Count = model.ExpiredSubscriptions }
            };

            model.SubscriptionsPerMonth = BuildMonthlyBuckets(
                scopedSubscriptions.Where(s => s.StartDate.Date >= rangeFrom && s.StartDate.Date <= rangeTo),
                rangeFrom,
                rangeTo,
                s => s.StartDate);

            var subscriberNames = subscribers.ToDictionary(s => s.Id, s => s.FullName);
            model.TopRenters = scopedRentals
                .Where(r => r.StartDate.Date >= rangeFrom && r.StartDate.Date <= rangeTo)
                .GroupBy(r => r.SubscriberId)
                .Select(g => new NamedCount
                {
                    Name = subscriberNames.TryGetValue(g.Key, out var name) ? name : string.Empty,
                    Count = g.Count()
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Name)
                .Take(10)
                .ToList();

            ViewBag.Filter = new SubscribersDashboardFilterViewModel
            {
                From = rangeFrom,
                To = rangeTo,
                GovernorateId = governorateId,
                Governorates = governorates.OrderBy(g => g.Name, StringComparer.Ordinal).ToList()
            };

            ViewBag.Dashboard = model;

            return View();
        }

        private List<NamedCount> BuildAgeGroups(List<Subscriber> subscribers, DateTime now)
        {
            var labels = new[]
            {
                _localizer["lbAgeUnder18"].Value,
                _localizer["lbAge18to25"].Value,
                _localizer["lbAge26to35"].Value,
                _localizer["lbAge36to50"].Value,
                _localizer["lbAgeOver50"].Value
            };

            var counts = new int[labels.Length];
            foreach (var subscriber in subscribers)
            {
                var age = AgeOn(subscriber.DateOfBirth, now);
                var bucket = age < 18 ? 0 : age <= 25 ? 1 : age <= 35 ? 2 : age <= 50 ? 3 : 4;
                counts[bucket]++;
            }

            return Enumerable.Range(0, labels.Length)
                .Select(i => new NamedCount { Name = labels[i], Count = counts[i] })
                .ToList();
        }

        private static int AgeOn(DateTime dateOfBirth, DateTime now)
        {
            var age = now.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > now.Date.AddYears(-age))
            {
                age--;
            }

            return Math.Max(age, 0);
        }

        private static List<NamedCount> BuildMonthlyBuckets<T>(
            IEnumerable<T> items,
            DateTime from,
            DateTime to,
            Func<T, DateTime> startOf)
        {
            var rows = items.ToList();

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
                        Count = rows.Count(x =>
                        {
                            var started = startOf(x);
                            return started.Year == month.Year && started.Month == month.Month;
                        })
                    };
                })
                .ToList();
        }
    }
}
