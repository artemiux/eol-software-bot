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
                throw new ArgumentException($"'{nameof(fromInclusive)}' must be less than or equal to '{nameof(toInclusive)}'.");
            }

            if (CountDays() > 31)
            {
                throw new ArgumentException($"The difference between '{nameof(fromInclusive)}' and '{nameof(toInclusive)}' must not exceed 31 days.");
            }

            return $"""
                {localizer["ReportHeader", lang]}:
                {CreateBody()}
                <i>{localizer["Source", lang]}: https://endoflife.date</i>
                """;

            double CountDays()
            {
                return (toInclusive - fromInclusive).TotalDays + 1;
            }

            string CreateBody()
            {
                var culture = lang is not null
                    && localizer.Cultures.Any(x => string.Equals(x.Name, lang, StringComparison.OrdinalIgnoreCase))
                    ? new CultureInfo(lang) : CultureInfo.InvariantCulture;

                var sb = new StringBuilder();
                var startDate = fromInclusive.Date;
                while (startDate <= toInclusive)
                {
                    sb.AppendLine();
                    sb.AppendLine(startDate.ToString("ddd, d MMM:", culture));
                    var matchedItems = items
                        .Where(item => item.Eol.Date == startDate.Date).ToArray();
                    if (matchedItems.Length > 0)
                    {
                        foreach (var item in matchedItems)
                        {
                            sb.AppendLine($"— <a href=\"{item.ProductUrl}\"><b>{item.ProductName} {item.ProductVersion}</b></a>");
                        }
                    }
                    else
                    {
                        sb.AppendLine(localizer["None", lang]);
                    }
                    startDate = startDate.AddDays(1);
                }

                return sb.ToString();
            }
        }
    }
}
