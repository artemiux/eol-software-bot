using EolBot.Models;
using EolBot.Services.Report;
using Microsoft.EntityFrameworkCore;

namespace EolBot.Repositories.Abstract
{
    public interface IReportRepository : IQueryableRepository<Report>
    {
        Task<Report> AddAsync(DateTime from, DateTime to, IEnumerable<ReportItem> content);

        #region Default implementation
        async Task<Report?> LastAsync() => await GetQueryable().Include(x => x.Content)
            .OrderBy(r => r.CreatedAt)
            .LastOrDefaultAsync();
        #endregion
    }
}
