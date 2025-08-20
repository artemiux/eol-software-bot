using EolBot.Models;

namespace EolBot.Repositories.Abstract
{
    public interface IReportRepository : IQueryableRepository<Report>
    {
        Task<Report> AddAsync(string text);

        Task<Report?> LastAsync();
    }
}
