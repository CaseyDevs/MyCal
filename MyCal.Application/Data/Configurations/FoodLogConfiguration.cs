using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCal.Domain.Entity;

namespace MyCal.Application.Data.Configurations;

public class FoodLogConfiguration : IEntityTypeConfiguration<FoodLog>
{
    public void Configure(EntityTypeBuilder<FoodLog> builder)
    {
        builder.HasKey(foodLog => foodLog.Id);

        builder.Property(foodLog => foodLog.UserId)
            .IsRequired();
        
        builder.HasIndex(foodLog => foodLog.UserId);

        builder.HasIndex(foodlog => new { foodlog.UserId, foodlog.Date })
            .IsUnique();
    }
}