using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCal.Domain.Entity;

namespace MyCal.Application.Data.Configurations;

public class FoodLogEntryConfiguration : IEntityTypeConfiguration<FoodLogEntry>
{
    public void Configure(EntityTypeBuilder<FoodLogEntry> builder)
    {
        builder.ToTable("FoodLogEntries");

        builder.HasOne(entry => entry.FoodLog)
            .WithMany(log => log.Entries)
            .HasForeignKey(entry => entry.FoodLogId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entry => entry.Food)
            .WithMany()
            .HasForeignKey(entry => entry.FoodId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
