using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCal.Domain.Enum;
using MyCal.Domain.Entity;

namespace MyCal.Application.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(user => user.Id);

        builder.Property(user => user.IdentityUserId)
            .IsRequired();

        builder.HasIndex(user => user.IdentityUserId)
           .IsUnique()
           .HasDatabaseName("IX_Users_IdentityUserId");

        builder.Property(user => user.OnboardingStatus)
            .HasDefaultValue(OnboardingStatus.Pending);
    }
}
