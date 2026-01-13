using EolBot.Resources;
using EolBot.Services.Localization.Abstract;
using System.Globalization;
using System.Resources;

namespace EolBot.Services.Localization
{
    public partial class LocalizationService(ILogger<LocalizationService> logger) : ILocalizationService
    {
        private readonly ResourceManager _manager = new(typeof(SharedResources));

        public IReadOnlyDictionary<string, CultureInfo> Cultures
        {
            get
            {
                field ??= CultureInfo
                    .GetCultures(CultureTypes.AllCultures)
                    .Where(x => _manager.GetResourceSet(x, true, false) is not null)
                    .ToDictionary(x => x.Name.ToLower());
                return field;
            }
        }

        public string GetString(string name, string? lang = null) => Read(name, lang);

        public string this[string name, string? lang = null] => Read(name, lang);

        private string Read(string name, string? lang = null)
        {
            var value = _manager.GetString(name, ((ILocalizationService)this).GetCultureOrDefault(lang));
            if (value is null)
            {
                LogMissingKey(logger, name);
            }
            return value ?? name;
        }

        #region Logging

        [LoggerMessage(LogLevel.Warning, "Missing localization key '{Key}'")]
        static partial void LogMissingKey(ILogger logger, string key);

        #endregion
    }
}
