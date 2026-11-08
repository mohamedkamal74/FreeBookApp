using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FreeBook.Controllers
{
    public class LocalizationController : Controller
    {
        private readonly RequestLocalizationOptions _localizationOptions;

        public LocalizationController(IOptions<RequestLocalizationOptions> localizationOptions)
        {
            _localizationOptions = localizationOptions.Value;
        }

        [HttpPost]
        public IActionResult SetCulture(string culture, string returnUrl)
        {
            var supportedCultures = _localizationOptions.SupportedUICultures?.Select(c => c.Name).ToArray()
                ?? new[] { "ar", "en" };

            if (string.IsNullOrEmpty(culture) || !supportedCultures.Contains(culture, StringComparer.OrdinalIgnoreCase))
            {
                culture = _localizationOptions.DefaultRequestCulture.UICulture.Name;
            }

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax
                });

            return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
        }
    }
}
