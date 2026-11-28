using System;

namespace Infrastructure.Common
{
    /// <summary>
    /// Known query-string filter values shared by admin list repositories.
    /// Keeping them here prevents controllers from owning filter predicates.
    /// </summary>
    public static class ListFilters
    {
        public const string Blacklisted = "blacklisted";
        public const string Active = "active";
        public const string Expiring = "expiring";
        public const string Overdue = "overdue";

        public static bool Matches(string? value, string filter) =>
            string.Equals(value, filter, StringComparison.OrdinalIgnoreCase);
    }
}
