using EolBot.Resources;
using EolBot.Services.Localization.Abstract;
using System.Globalization;
using System.Resources;

namespace EolBot.Services.Localization
{
    public class LocalizationService : ILocalizationService
    {
        private readonly ResourceManager _manager;
        private readonly List<CultureInfo> _availableCultures;
        private readonly ILogger<LocalizationService> _logger;

        public IEnumerable<CultureInfo> Cultures => _availableCultures;

        public LocalizationService(ILogger<LocalizationService> logger)
        {
            _manager = new ResourceManager(typeof(SharedResources));
            _availableCultures = [.. CultureInfo
                .GetCultures(CultureTypes.AllCultures)
                .Where(x => _manager.GetResourceSet(x, true, false) != null)];
            _logger = logger;
        }

        public string GetString(string name, string? lang = null)
        {
            return Read(name, lang);
        }

        public string this[string name] => Read(name);

        private string Read(string name, string? lang = null)
        {
            var value = _manager.GetString(name,
                lang != null ? new CultureInfo(lang) : null);
            if (value == null)
            {
                _logger.LogWarning("Missing localization key '{Key}'", name);
            }
            return value ?? name;
        }
    }
}
