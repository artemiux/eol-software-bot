using EolBot.Services.Report.Abstract;
using System.Globalization;
using System.Text;

namespace EolBot.Services.Report.EndoflifeDate
{
    public class EndoflifeDateDailyReport : IReport
    {
        public string Create(DateTime fromInclusive, DateTime toInclusive,
            IEnumerable<ReportItem> items)
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
                End-of-life (EOL) calendar for the next {CountDays(fromInclusive, toInclusive)} days:
                {CreateBody(fromInclusive, toInclusive, items)}
                <i>Source: https://endoflife.date</i>
                """;
        }

        private string CreateBody(DateTime fromInclusive, DateTime toInclusive,
            IEnumerable<ReportItem> items)
        {
            var sb = new StringBuilder();

            var startDate = fromInclusive.Date;
            while (startDate <= toInclusive)
            {
                sb.AppendLine();
                sb.AppendLine(startDate.ToString("ddd, dd MMM:", CultureInfo.InvariantCulture));
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
                    sb.AppendLine("None");
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
