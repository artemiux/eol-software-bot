using EolBot.Models;
using EolBot.Services.Report;

namespace EolBot.Repositories.Abstract
{
    public interface IReportRepository : IQueryableRepository<Report>
    {
        Task<Report> AddAsync(DateTime from, DateTime to, IEnumerable<ReportItem> content);

        Task<Report?> LastAsync();
    }
}
