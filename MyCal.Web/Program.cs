using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyCal.Application.Data;
using MyCal.Application.Tracking;
using MyCal.Infrastructure.Identity;
using MyCal.Infrastructure.Nutrition;
using MyCal.Shared.Tracking;
using MyCal.Web;
using MyCal.Web.Components;
using MyCal.Web.Security;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
// Reuse only the prototype's nutrition key, never its database connection settings.
if (builder.Environment.IsDevelopment() && string.IsNullOrEmpty(builder.Configuration["UsdaFoodData:ApiKey"]))
{
    var prototype = new ConfigurationBuilder().AddUserSecrets("8e0c1884-2680-4cc4-b3f8-5f21fe1def11", reloadOnChange: false).Build();
    if (prototype["UsdaFoodData:ApiKey"] is { } key)
        builder.Configuration["UsdaFoodData:ApiKey"] = key;
}
builder.AddServiceDefaults();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddRazorPages();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingAuthenticationStateProvider>();
builder.Services.AddScoped<CurrentAccount>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<TrackerService>();
builder.Services.AddScoped<BetaAccounts>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient("nutrition", c => c.Timeout = TimeSpan.FromSeconds(12)).RemoveAllLoggers();
builder.Services.AddSingleton<IFoodSearch, FoodCatalog>();
builder.Services.AddProblemDetails();
builder.Services.AddAntiforgery(o => o.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("accounts", context => !HttpMethods.IsPost(context.Request.Method)
        ? System.Threading.RateLimiting.RateLimitPartition.GetNoLimiter("read")
        : System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "local",
            _ => new() { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

var appConnection = builder.Configuration.GetConnectionString("postgresdb")
    ?? DatabaseConnection.Parse(builder.Configuration["DATABASE_URL"])
    ?? "Host=localhost;Port=55432;Database=mycal;Username=mycal;Password=local-development-only";
var identityConnection = builder.Configuration.GetConnectionString("identitydb")
    ?? new NpgsqlConnectionStringBuilder(appConnection) { Database = "identitydb" }.ConnectionString;
builder.Services.AddDbContextFactory<AppDbContext>(o => o.UseNpgsql(appConnection,
    p => p.MigrationsAssembly(typeof(MyCal.Infrastructure.InfrastructureAssemblyMaker).Assembly.FullName)));
builder.Services.AddDbContextFactory<ApplicationDbContext>((sp, o) => o.UseApplicationServiceProvider(sp).UseNpgsql(identityConnection));
builder.Services.AddAuthentication(o =>
{
    o.DefaultScheme = IdentityConstants.ApplicationScheme;
    o.DefaultSignInScheme = IdentityConstants.ExternalScheme;
}).AddIdentityCookies();
builder.Services.AddIdentityCore<ApplicationUser>(o =>
{
    o.Password.RequiredLength = 12;
    o.User.RequireUniqueEmail = true;
    o.SignIn.RequireConfirmedAccount = true;
    o.Lockout.MaxFailedAccessAttempts = 5;
    o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    o.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
}).AddEntityFrameworkStores<ApplicationDbContext>().AddSignInManager().AddDefaultTokenProviders();
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.Zero);
builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/account/login";
    o.AccessDeniedPath = "/account/login";
    o.Cookie.Name = "MyCal.Session";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    o.ExpireTimeSpan = TimeSpan.FromDays(14);
    o.SlidingExpiration = true;
    o.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        else
            context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    o.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
        else
            context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});
builder.Services.AddAuthorization();
var protection = builder.Services.AddDataProtection().SetApplicationName("MyCal");
var keyPath = builder.Configuration["DataProtection:KeyPath"];
if (!string.IsNullOrWhiteSpace(keyPath))
    protection.PersistKeysToFileSystem(new DirectoryInfo(keyPath));
else if (!builder.Environment.IsDevelopment())
    throw new InvalidOperationException("Configure DataProtection__KeyPath to a persistent volume before production startup.");
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Production ingress must be the only route to the container (Caddy or Railway).
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
    o.ForwardLimit = 1;
});

var app = builder.Build();
if (args.Contains("--migrate") || app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await DatabaseConnection.EnsureDatabaseAsync(identityConnection);
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
    if (args.Contains("--migrate"))
        return;
}
if (args.Contains("--bootstrap-admin"))
{
    var email = builder.Configuration["Bootstrap:Email"] ?? throw new InvalidOperationException("Set Bootstrap__Email.");
    var password = builder.Configuration["Bootstrap:Password"] ?? throw new InvalidOperationException("Set Bootstrap__Password.");
    await using var scope = app.Services.CreateAsyncScope();
    var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var user = await manager.FindByEmailAsync(email);
    if (user is null)
    {
        user = new ApplicationUser { Email = email, UserName = email, EmailConfirmed = true, IsAdmin = true };
        var result = await manager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(x => x.Description)));
    }
    else
    {
        user.IsAdmin = true;
        user.EmailConfirmed = true;
        var result = await manager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException("Unable to grant administrator access.");
    }
    Console.WriteLine("Administrator account is ready. Remove the bootstrap password from your environment.");
    return;
}
app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Error"); app.UseHsts(); app.UseHttpsRedirection(); }
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers.CacheControl = "no-cache, no-store";
    context.Response.Headers.Pragma = "no-cache";
    await next();
});
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseAntiforgery();
app.MapStaticAssets().AllowAnonymous();
app.MapRazorPages().RequireRateLimiting("accounts");
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapPost("/account/logout", async (HttpContext context, SignInManager<ApplicationUser> manager,
    Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery) =>
{
    await antiforgery.ValidateRequestAsync(context);
    await manager.SignOutAsync();
    return Results.LocalRedirect("/account/login");
}).RequireAuthorization();
app.MapTrackerApi();
app.MapDefaultEndpoints();
app.Run();

public partial class Program;

internal static class DatabaseConnection
{
    public static string? Parse(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;
        var uri = new Uri(url);
        var credentials = uri.UserInfo.Split(':', 2);
        return new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.Trim('/'),
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : ""
        }.ConnectionString;
    }
    public static async Task EnsureDatabaseAsync(string connection)
    {
        var target = new NpgsqlConnectionStringBuilder(connection);
        var admin = new NpgsqlConnectionStringBuilder(connection) { Database = "postgres" };
        await using var cn = new NpgsqlConnection(admin.ConnectionString);
        await cn.OpenAsync();
        await using var check = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", cn);
        check.Parameters.AddWithValue("name", target.Database!);
        if (await check.ExecuteScalarAsync() is null)
        {
            await using var create = new NpgsqlCommand("CREATE DATABASE \"" + target.Database!.Replace("\"", "\"\"") + "\"", cn);
            await create.ExecuteNonQueryAsync();
        }
    }
}
