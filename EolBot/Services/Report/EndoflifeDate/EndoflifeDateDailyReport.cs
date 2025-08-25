using EolBot.Services.Localization.Abstract;
using EolBot.Services.Report.Abstract;
using System.Globalization;
using System.Text;

namespace EolBot.Services.Report.EndoflifeDate
{
    public class EndoflifeDateDailyReport(ILocalizationService localizer) : IReport
    {
        public string Create(DateTime fromInclusive, DateTime toInclusive,
            IEnumerable<ReportItem> items, string? lang = null)
        {
            if (fromInclusive > toInclusive)
            {
                throw new ArgumentException($"The '{nameof(fromInclusive)}' must be less than or equal to the '{nameof(toInclusive)}'.");
            }

            if (CountDays(fromInclusive, toInclusive) > 31)
            {
                throw new ArgumentException($"The difference between '{nameof(fromInclusive)}' and '{nameof(toInclusive)}' must not exceed 31 days.");
            }

            return $"""
                {string.Format(localizer.GetString("ReportHeader", lang), CountDays(fromInclusive, toInclusive))}:
                {CreateBody(fromInclusive, toInclusive, items, lang)}
                <i>{localizer.GetString("Source", lang)}: https://endoflife.date</i>
                """;
        }

        private string CreateBody(DateTime fromInclusive, DateTime toInclusive,
            IEnumerable<ReportItem> items, string? lang = null)
        {
            var culture = lang != null
                && localizer.Cultures.Any(x => string.Equals(x.Name, lang, StringComparison.OrdinalIgnoreCase))
                ? new CultureInfo(lang) : CultureInfo.InvariantCulture;

            var sb = new StringBuilder();
            var startDate = fromInclusive.Date;
            while (startDate <= toInclusive)
            {
                sb.AppendLine();
                sb.AppendLine(startDate.ToString("ddd, dd MMM:", culture));
                var matchedItems = items
                    .Where(item => item.Eol.Date == startDate.Date);
                if (matchedItems.Any())
                {
                    foreach (var item in matchedItems)
                    {
                        sb.AppendLine($"— <a href=\"{item.ProductUrl}\"><b>{item.ProductName} {item.ProductVersion}</b></a>");
                    }
                }
                else
                {
                    sb.AppendLine(localizer.GetString("None", lang));
                }
                startDate = startDate.AddDays(1);
            }

            return sb.ToString();
        }

        private double CountDays(DateTime fromInclusive, DateTime toInclusive)
        {
            return (toInclusive - fromInclusive).TotalDays + 1;
        }
    }
}
