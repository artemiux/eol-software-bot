using EolBot.Models;
using EolBot.Services.Report;

namespace EolBot.Extensions
{
    static class ReportContentExtensions
    {
        extension(ReportContent content)
        {
            internal ReportItem ConvertToReportItem() => new()
            {
                ProductName = content.ProductName,
                ProductVersion = content.ProductVersion,
                ProductUrl = content.ProductUrl,
                Eol = content.Eol
            };
        }
    }
}
