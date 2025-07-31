using EolBot.Models;
using System.Linq.Expressions;

namespace EolBot.Repositories.Abstract
{
    public interface IUserRepository : IQueryableRepository<User>
    {
        Task<PaginatedResult<User>> GetAsync(
            Expression<Func<User, bool>>? filter = null, int? start = 1, int? limit = 50);

        Task SubscribeAsync(long telegramId);

        Task UnsubscribeAsync(long telegramId);

        Task<StatsItem> GetStatsAsync();

        Task<StatsItem> GetStatsAsync(DateTime fromInclusive, DateTime toInclusive);
    }

    public record StatsItem(int TotalUsers, int ActiveUsers);
}
