using EolBot.Database;
using EolBot.Models;
using EolBot.Repositories.Abstract;
using Microsoft.EntityFrameworkCore;

namespace EolBot.Repositories
{
    public class DatabaseReportRepository(EolBotDbContext context) : IReportRepository, IDisposable
    {
        private bool disposedValue;

        public async Task<Report> AddAsync(string text)
        {
            var report = new Report
            {
                Data = text
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
            return GetQueryable()
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
