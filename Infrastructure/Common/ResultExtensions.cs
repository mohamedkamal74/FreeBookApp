namespace Infrastructure.Common
{
    public static class ResultExtensions
    {
        /// <summary>Returns the successful value, or the supplied fallback for failed/empty results.</summary>
        public static T ValueOr<T>(this Result<T> result, T fallback) where T : class
            => result.IsSuccess ? result.Value ?? fallback : fallback;
    }
}
