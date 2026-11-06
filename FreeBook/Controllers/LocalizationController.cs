using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace FreeBook.Controllers
{
    public class LocalizationController : Controller
    {
        [HttpPost]
        public IActionResult SetCulture(string culture, string returnUrl)
        {
            var supportedCultures = new[] { "ar", "en" };
            if (!supportedCultures.Contains(culture))
            {
                culture = "ar";
            }

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true
                });

            return LocalRedirect(returnUrl ?? "/");
        }
    }
}
