using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCal.ApiService.Common.Enum;
using MyCal.ApiService.Common.Model;

namespace MyCal.ApiService.Data.Configurations;

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
