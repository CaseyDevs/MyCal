using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MyCal.Infrastructure.Identity;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<BetaLink> BetaLinks => Set<BetaLink>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<BetaLink>().HasIndex(x => x.TokenHash).IsUnique();
        builder.Entity<BetaLink>().Property(x => x.TokenHash).HasMaxLength(64);
        builder.Entity<BetaLink>().Property(x => x.Email).HasMaxLength(256);
    }
}
