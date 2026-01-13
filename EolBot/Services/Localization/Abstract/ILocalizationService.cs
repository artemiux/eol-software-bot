using System.Globalization;

namespace EolBot.Services.Localization.Abstract
{
    public interface ILocalizationService
    {
        IReadOnlyDictionary<string, CultureInfo> Cultures { get; }

        CultureInfo? GetCultureOrDefault(string? lang) => lang switch
        {
            string key when Cultures.TryGetValue(key, out var culture) => culture,
            _ => default
        };

        string GetString(string name, string? lang = null);

        string this[string name, string? lang = null] { get; }
    }
}
