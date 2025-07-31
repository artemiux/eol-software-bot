using EolBot.Models;
using Microsoft.EntityFrameworkCore;

namespace EolBot.Database
{
    public class EolBotDbContext(IConfiguration configuration) : DbContext
    {
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseSqlite(configuration.GetConnectionString("DefaultConnection"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasKey(u => u.TelegramId);

            modelBuilder.Entity<User>()
                .Property(u => u.TelegramId)
                .ValueGeneratedNever();

            modelBuilder.Entity<User>()
                .Property(u => u.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}
