using EolBot.Resources;
using EolBot.Services.Localization.Abstract;
using System.Globalization;
using System.Resources;

namespace EolBot.Services.Localization
{
    public class LocalizationService(ILogger<LocalizationService> logger) : ILocalizationService
    {
        private readonly ResourceManager _manager = new(typeof(SharedResources));

        public IEnumerable<CultureInfo> Cultures
        {
            get
            {
                field ??= [.. CultureInfo
                    .GetCultures(CultureTypes.AllCultures)
                    .Where(x => _manager.GetResourceSet(x, true, false) is not null)];
                return field;
            }
        }

        public string GetString(string name, string? lang = null)
        {
            return Read(name, lang);
        }

        public string this[string name, string? lang = null] => Read(name, lang);

        private string Read(string name, string? lang = null)
        {
            var value = _manager.GetString(name,
                lang is not null ? new CultureInfo(lang) : null);
            if (value is null)
            {
                logger.LogWarning("Missing localization key '{Key}'", name);
            }
            return value ?? name;
        }
    }
}
