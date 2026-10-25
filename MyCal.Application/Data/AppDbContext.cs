using Microsoft.EntityFrameworkCore;
using MyCal.Domain.Entity;

namespace MyCal.Application.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Food> Foods => Set<Food>();
    public DbSet<FoodLog> FoodLogs => Set<FoodLog>();
    public DbSet<FoodLogEntry> FoodLogEntries => Set<FoodLogEntry>();
    public DbSet<TargetRevision> Targets => Set<TargetRevision>();
    public DbSet<WeightEntry> Weights => Set<WeightEntry>();
    public DbSet<SavedFood> SavedFoods => Set<SavedFood>();
    public DbSet<CoachingReview> Reviews => Set<CoachingReview>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Scans classes in assembly for configuration implementations
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);
    }
}
