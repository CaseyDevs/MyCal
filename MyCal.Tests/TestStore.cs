using Microsoft.EntityFrameworkCore;
using MyCal.Application.Data;
using MyCal.Application.Tracking;
using MyCal.Infrastructure.Identity;
using Npgsql;

namespace MyCal.Tests;

public sealed class TestStore : IAsyncDisposable
{
    public string AppConnection { get; private set; } = "";
    public string IdentityConnection { get; private set; } = "";
    public FixedClock Clock { get; } = new(DateTimeOffset.Parse("2026-10-25T12:00:00Z"));
    public IDbContextFactory<AppDbContext> Factory => new TestFactory(AppConnection);
    public TrackerService Tracker => new(Factory, Clock);
    public static async Task<TestStore> CreateAsync(bool migrate = true)
    {
        var store = new TestStore();
        var root = Environment.GetEnvironmentVariable("MYCAL_TEST_POSTGRES") ?? "Host=localhost;Port=55432;Username=mycal;Password=local-development-only;Database=postgres;Timeout=5";
        var suffix = Guid.NewGuid().ToString("N");
        store.AppConnection = new NpgsqlConnectionStringBuilder(root) { Database = "mycal_test_" + suffix }.ConnectionString;
        store.IdentityConnection = new NpgsqlConnectionStringBuilder(root) { Database = "mycal_identity_test_" + suffix }.ConnectionString;
        await using var connection = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(root) { Database = "postgres" }.ConnectionString);
        await connection.OpenAsync();
        foreach (var text in new[] { store.AppConnection, store.IdentityConnection })
        {
            var name = new NpgsqlConnectionStringBuilder(text).Database;
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", connection);
            await create.ExecuteNonQueryAsync();
        }
        if (migrate)
        {
            await using (var db = await store.Factory.CreateDbContextAsync())
                await db.Database.MigrateAsync();
            await using (var db = MyCal.Infrastructure.DesignTimeIdentityContext.Create(store.IdentityConnection))
                await db.Database.MigrateAsync();
        }
        return store;
    }
    public async ValueTask DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();
        var root = new NpgsqlConnectionStringBuilder(AppConnection) { Database = "postgres" };
        await using var connection = new NpgsqlConnection(root.ConnectionString);
        await connection.OpenAsync();
        foreach (var text in new[] { AppConnection, IdentityConnection })
        {
            var name = new NpgsqlConnectionStringBuilder(text).Database!;
            if (!name.StartsWith("mycal_test_") && !name.StartsWith("mycal_identity_test_"))
                throw new InvalidOperationException("Refusing to drop a non-test database.");
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{name}\" WITH (FORCE)", connection);
            await drop.ExecuteNonQueryAsync();
        }
    }
    private sealed class TestFactory(string connection) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection,
            x => x.MigrationsAssembly(typeof(MyCal.Infrastructure.InfrastructureAssemblyMaker).Assembly.FullName)).Options);
    }
}
