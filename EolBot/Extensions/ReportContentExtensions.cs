using EolBot.Models;
using EolBot.Services.Report;

namespace EolBot.Extensions
{
    static class ReportContentExtensions
    {
        internal static ReportItem ConvertToReportItem(this ReportContent content)
        {
            return new ReportItem
            {
                ProductName = content.ProductName,
                ProductVersion = content.ProductVersion,
                ProductUrl = content.ProductUrl,
                Eol = content.Eol
            };
        }
    }
}
