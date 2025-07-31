using EolBot.Database;
using EolBot.Models;
using EolBot.Repositories.Abstract;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace EolBot.Repositories
{
    public class DatabaseUserRepository(EolBotDbContext context) : IUserRepository, IDisposable
    {
        private bool disposedValue;

        public virtual IQueryable<User> GetQueryable()
        {
            return context.Users.AsNoTracking();
        }

        public async Task<PaginatedResult<User>> GetAsync(Expression<Func<User, bool>>? filter = null,
            int? start = 1, int? limit = 50)
        {
            if (start < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Must be greater than or equal to 1.");
            }
            if (limit < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(limit), "Must be greater than or equal to 1.");
            }

            var query = GetQueryable();
            if (filter != null)
            {
                query = query.Where(filter);
            }
            int skip = start == 1 ? 0 : start.GetValueOrDefault() - 1;
            int take = limit.GetValueOrDefault();
            var items = await query.Skip(skip).Take(take).ToListAsync();
            int total = await query.CountAsync();
            int last = skip + take;
            return new PaginatedResult<User>
            {
                Next = total > last ? ++last : null,
                Result = items
            };
        }

        public async Task SubscribeAsync(long telegramId)
        {
            var user = await context.Users.FindAsync(telegramId);
            var now = DateTime.UtcNow;
            if (user == null)
            {
                context.Users.Add(new User
                {
                    TelegramId = telegramId,
                    IsActive = true,
                    SubscribedAt = now,
                    CreatedAt = now
                });
            }
            else if (!user.IsActive)
            {
                user.IsActive = true;
                user.SubscribedAt = now;
            }
            await context.SaveChangesAsync();
        }

        public async Task UnsubscribeAsync(long telegramId)
        {
            var user = await context.Users.FindAsync(telegramId);
            if (user == null)
            {
                context.Users.Add(new User
                {
                    TelegramId = telegramId,
                    IsActive = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else if (user.IsActive)
            {
                user.IsActive = false;
                user.SubscribedAt = null;
            }
            await context.SaveChangesAsync();
        }

        public async Task<StatsItem> GetStatsAsync()
        {
            return await GetStatsAsync(DateTime.MinValue, DateTime.MaxValue);
        }

        public async Task<StatsItem> GetStatsAsync(DateTime fromInclusive, DateTime toInclusive)
        {
            if (fromInclusive > toInclusive)
            {
                throw new ArgumentException($"Must be less than or equal to the '{nameof(toInclusive)}'.",
                    nameof(fromInclusive));
            }

            var totalUsers = await GetQueryable().CountAsync(u => u.CreatedAt >= fromInclusive
                && u.CreatedAt <= toInclusive);
            var activeUsers = await GetQueryable().CountAsync(u => u.IsActive
                && u.SubscribedAt >= fromInclusive && u.SubscribedAt <= toInclusive);
            return new StatsItem(totalUsers, activeUsers);
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
