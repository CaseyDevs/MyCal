using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MyCal.Application.Data;
using MyCal.Infrastructure.Identity;

namespace MyCal.Infrastructure;

// Migration scaffolding compares models without starting the web server or opening a database.
public sealed class DesignTimeAppContext : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql("Host=localhost;Database=mycal;Username=mycal", p => p.MigrationsAssembly(typeof(DesignTimeAppContext).Assembly.FullName)).Options);
}
public sealed class DesignTimeIdentityContext : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    // Identity selects its schema from the application service provider, including at design time.
    private static readonly IServiceProvider SchemaServices = new ServiceCollection()
        .Configure<IdentityOptions>(o => o.Stores.SchemaVersion = IdentitySchemaVersions.Version3).BuildServiceProvider();
    public ApplicationDbContext CreateDbContext(string[] args) => Create("Host=localhost;Database=identitydb;Username=mycal");
    public static ApplicationDbContext Create(string connection) => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseApplicationServiceProvider(SchemaServices).UseNpgsql(connection).Options);
}
