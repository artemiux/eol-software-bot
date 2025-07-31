using EolBot.Models;
using EolBot.Services.Telegram;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EolBot.Database
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(EolBotDbContext context,
            IOptions<TelegramSettings> options, bool deleteExisting = true)
        {
            if (deleteExisting)
            {
                context.Users.ExecuteDelete();
            }

            if (deleteExisting || !context.Users.Any())
            {
                var now = DateTime.UtcNow;
                context.Users.Add(new User
                {
                    TelegramId = options.Value.AdminChatId,
                    IsActive = true,
                    SubscribedAt = now,
                    CreatedAt = now
                });
                await context.SaveChangesAsync();
            }
        }
    }
}
