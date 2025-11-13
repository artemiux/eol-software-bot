using EolBot.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace EolBot.Repositories.Abstract
{
    public interface IUserRepository : IQueryableRepository<User>
    {
        Task<PaginatedResult<User>> GetAsync(
            Expression<Func<User, bool>>? filter = null, int start = 1, int limit = 50);

        Task SubscribeAsync(long telegramId, string? lang = null);

        Task UnsubscribeAsync(long telegramId, string? lang = null);

        #region Default implementation
        async Task<int> GetTotalAsync() => await GetQueryable().CountAsync();

        async Task<int> GetTotalAsync(DateTime fromInclusive, DateTime toInclusive)
        {
            if (fromInclusive > toInclusive)
            {
                throw new ArgumentException($"Must be less than or equal to the '{nameof(toInclusive)}'.",
                    nameof(fromInclusive));
            }

            return await GetQueryable().CountAsync(u => u.CreatedAt >= fromInclusive
                && u.CreatedAt <= toInclusive);
        }

        async Task<int> GetActiveAsync() => await GetQueryable().CountAsync(u => u.IsActive);

        async Task<int> GetActiveAsync(DateTime fromInclusive, DateTime toInclusive)
        {
            if (fromInclusive > toInclusive)
            {
                throw new ArgumentException($"Must be less than or equal to the '{nameof(toInclusive)}'.",
                    nameof(fromInclusive));
            }

            return await GetQueryable().CountAsync(u => u.IsActive
                && u.SubscribedAt >= fromInclusive && u.SubscribedAt <= toInclusive);
        }
        #endregion
    }
}
