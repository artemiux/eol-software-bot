using EolBot.Database;
using EolBot.Models;
using EolBot.Repositories.Abstract;
using EolBot.Services.Report;
using Microsoft.EntityFrameworkCore;

namespace EolBot.Repositories
{
    public class DatabaseReportRepository(EolBotDbContext context) : IReportRepository, IDisposable
    {
        private bool disposedValue;

        public async Task<Report> AddAsync(DateTime from, DateTime to, IEnumerable<ReportItem> content)
        {
            var report = new Report
            {
                From = from,
                To = to,
                Content = [.. content.Select(x => new ReportContent(x.ProductName, x.ProductVersion, x.Eol, x.ProductUrl))]
            };
            context.Add(report);
            await context.SaveChangesAsync();
            return report;
        }

        public IQueryable<Report> GetQueryable()
        {
            return context.Reports.AsNoTracking();
        }

        public Task<Report?> LastAsync()
        {
            return GetQueryable().Include(x => x.Content)
                .OrderBy(r => r.CreatedAt)
                .LastOrDefaultAsync();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    context.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
