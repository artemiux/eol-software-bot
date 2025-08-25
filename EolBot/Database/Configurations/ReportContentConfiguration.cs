using EolBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EolBot.Database.Configurations
{
    public class ReportContentConfiguration : IEntityTypeConfiguration<ReportContent>
    {
        public void Configure(EntityTypeBuilder<ReportContent> builder)
        {
            builder
                .Property(x => x.ProductName)
                .HasMaxLength(256);

            builder
                .Property(x => x.ProductVersion)
                .HasMaxLength(256);

            builder
                .Property(x => x.ProductUrl)
                .HasMaxLength(512);

            builder
                .Property(x => x.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}
