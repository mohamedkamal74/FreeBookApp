using System.Globalization;
using System.Resources;
using Microsoft.Extensions.Localization;

namespace FreeBook.Localization
{
    public class FreeBookStringLocalizer : IStringLocalizer
    {
        private readonly ResourceManager _resourceManager;

        public FreeBookStringLocalizer()
        {
            _resourceManager = FreeBook.Resources.ResourceWeb.ResourceManager;
        }

        public LocalizedString this[string name] => new LocalizedString(name, _resourceManager.GetString(name) ?? name);

        public LocalizedString this[string name, params object[] arguments] => new LocalizedString(name, _resourceManager.GetString(name) ?? name);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            _resourceManager.GetResourceSet(System.Globalization.CultureInfo.CurrentUICulture, true, true)?
                .Cast<System.Collections.DictionaryEntry>()
                .Select(e => new LocalizedString((string)e.Key, (string?)e.Value ?? string.Empty))
            ?? Enumerable.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(CultureInfo culture) => this;
    }

    public class FreeBookStringLocalizerFactory : IStringLocalizerFactory
    {
        public IStringLocalizer Create(Type resourceSource) => new FreeBookStringLocalizer();

        public IStringLocalizer Create(string baseName, string location) => new FreeBookStringLocalizer();
    }
}
