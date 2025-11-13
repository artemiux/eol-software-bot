using System.Globalization;

namespace EolBot.Services.Localization.Abstract
{
    public interface ILocalizationService
    {
        IEnumerable<CultureInfo> Cultures { get; }

        string GetString(string name, string? lang = null);

        string this[string name, string? lang = null] { get; }
    }
}
