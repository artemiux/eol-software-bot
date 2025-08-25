using EolBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EolBot.Database.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder
                .HasKey(x => x.TelegramId);

            builder
                .Property(x => x.TelegramId)
                .ValueGeneratedNever();

            builder
                .Property(x => x.LanguageCode)
                .HasMaxLength(5);

            builder
                .Property(x => x.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}
