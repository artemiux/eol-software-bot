using EolBot.Models;
using Microsoft.EntityFrameworkCore;

namespace EolBot.Database
{
    public class EolBotDbContext(IConfiguration configuration) : DbContext
    {
        public DbSet<User> Users { get; set; }

        public DbSet<Report> Reports { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseSqlite(configuration.GetConnectionString("DefaultConnection"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(User).Assembly);
        }
    }
}
