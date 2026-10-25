using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCal.Domain.Entity;

namespace MyCal.Application.Data.Configurations;

public sealed class TrackingConfiguration : IEntityTypeConfiguration<TargetRevision>,
    IEntityTypeConfiguration<WeightEntry>, IEntityTypeConfiguration<SavedFood>,
    IEntityTypeConfiguration<CoachingReview>
{
    public void Configure(EntityTypeBuilder<TargetRevision> b)
    {
        b.HasIndex(x => new { x.UserId, x.EffectiveFrom }).IsUnique();
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
    public void Configure(EntityTypeBuilder<WeightEntry> b)
    {
        b.HasIndex(x => new { x.UserId, x.Date }).IsUnique();
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
    public void Configure(EntityTypeBuilder<SavedFood> b)
    {
        b.HasIndex(x => new { x.UserId, x.Fingerprint }).IsUnique();
        b.Property(x => x.Name).HasMaxLength(200);
        b.Property(x => x.Fingerprint).HasMaxLength(64);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
    public void Configure(EntityTypeBuilder<CoachingReview> b)
    {
        b.HasIndex(x => new { x.UserId, x.Date, x.Fingerprint }).IsUnique();
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
