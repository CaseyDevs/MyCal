using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using MyCal.Application.Tracking;
using MyCal.Infrastructure.Identity;
using MyCal.Shared.Tracking;

namespace MyCal.Tests;

[TestClass, DoNotParallelize]
public sealed class WebTests
{
    [TestMethod]
    public async Task NutritionProviderFailuresDoNotTriggerHiddenRetries()
    {
        var calls = 0;
        using var app = _app.WithWebHostBuilder(b =>
        {
            b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(
                new Dictionary<string, string?> { ["UsdaFoodData:ApiKey"] = "test-key" }));
            b.ConfigureServices(s => s.AddHttpClient("nutrition").ConfigurePrimaryHttpMessageHandler(
                () => new UnavailableHandler(() => Interlocked.Increment(ref calls))));
        });
        var search = app.Services.GetRequiredService<IFoodSearch>();
        var result = await search.SearchAsync("provider outage");
        Assert.AreEqual(2, calls, "Each provider gets one request; retries must not bypass its quota.");
        Assert.AreEqual(0, result.Foods.Count);
        Assert.IsNotNull(result.Notice);
    }
    private sealed class UnavailableHandler(Action called) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            called();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        }
    }
    private static TestStore _store = null!;
    private static WebApplicationFactory<Program> _app = null!;
    private static readonly List<(string Id, string Email)> People = [];
    private static string? _oldApp, _oldIdentity;
    private const string Password = "Local-Test-Only-2026!";
    [ClassInitialize]
    public static async Task Setup(TestContext _)
    {
        _store = await TestStore.CreateAsync();
        _oldApp = Environment.GetEnvironmentVariable("ConnectionStrings__postgresdb");
        _oldIdentity = Environment.GetEnvironmentVariable("ConnectionStrings__identitydb");
        Environment.SetEnvironmentVariable("ConnectionStrings__postgresdb", _store.AppConnection);
        Environment.SetEnvironmentVariable("ConnectionStrings__identitydb", _store.IdentityConnection);
        _app = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.ConfigureServices(s => { s.RemoveAll<TimeProvider>(); s.AddSingleton<TimeProvider>(_store.Clock); });
        });
        await using var scope = _app.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tracker = scope.ServiceProvider.GetRequiredService<TrackerService>();
        for (var i = 0; i < 5; i++)
        {
            var email = $"person-{i}@example.com";
            var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, IsAdmin = i == 0 };
            var result = await users.CreateAsync(user, Password);
            Assert.IsTrue(result.Succeeded);
            People.Add((user.Id, email));
            await tracker.SaveProfileAsync(user.Id, email, new()
            {
                Name = $"Person {i}"
            });
        }
    }
    [ClassCleanup]
    public static async Task Cleanup()
    {
        await _app.DisposeAsync();
        await _store.DisposeAsync();
        Environment.SetEnvironmentVariable("ConnectionStrings__postgresdb", _oldApp);
        Environment.SetEnvironmentVariable("ConnectionStrings__identitydb", _oldIdentity);
    }
    private static async Task<HttpClient> Login(int person)
    {
        var client = _app.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        var html = await client.GetStringAsync("/account/login");
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.IsFalse(string.IsNullOrEmpty(token), "The login form must include a CSRF token.");
        var response = await client.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = People[person].Email,
            ["Password"] = Password,
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token)
        }));
        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        var csrf = await client.GetFromJsonAsync<Dictionary<string, string>>("/api/antiforgery");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf!["token"]);
        return client;
    }
    [TestMethod]
    public async Task AnonymousRequestsAndMissingCsrfCannotMutatePrivateData()
    {
        using var anonymous = _app.CreateClient(new()
        {
            AllowAutoRedirect = false
        });
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/profile")).StatusCode);
        using var member = await Login(1);
        member.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        Assert.AreEqual(HttpStatusCode.BadRequest, (await member.PutAsJsonAsync("/api/weights/2026-10-25", new
        {
            kilograms = 76
        })).StatusCode);
    }
    [TestMethod]
    public async Task FiveRealLoginsAndTenConcurrentSessionsRemainIndependent()
    {
        var clients = await Task.WhenAll(Enumerable.Range(0, 10).Select(i => Login(i % 5)));
        try
        {
            var results = await Task.WhenAll(clients.Select(x => x.GetFromJsonAsync<Profile>("/api/profile")));
            for (var i = 0; i < 10; i++)
                Assert.AreEqual(People[i % 5].Email, results[i]!.Email);
        }
        finally { foreach (var c in clients) c.Dispose(); }
    }
    [TestMethod]
    public async Task IdentityOverrideAndForeignEntryIdsDoNotExposeOrModifyOtherUsers()
    {
        using var owner = await Login(1);
        using var other = await Login(2);
        var posted = await owner.PostAsJsonAsync("/api/diary/2026-10-25/entries", new
        {
            meal = 0,
            amount = 100,
            food = new FoodItem("Private food", null, "Custom", null, null, MyCal.Domain.Entity.NutritionUnit.Grams, 100, 123, 4, 10, 2)
        });
        Assert.AreEqual(HttpStatusCode.OK, posted.StatusCode);
        var entry = (await posted.Content.ReadFromJsonAsync<Dictionary<string, int>>())!["id"];
        Assert.AreEqual(HttpStatusCode.NotFound, (await other.PutAsJsonAsync($"/api/entries/{entry}", new
        {
            meal = 1,
            amount = 200
        })).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await other.DeleteAsync($"/api/entries/{entry}")).StatusCode);
        var self = await other.GetFromJsonAsync<Profile>("/api/profile?identityUserId=" + People[1].Id);
        Assert.AreEqual(People[2].Email, self!.Email);
        var day = await other.GetFromJsonAsync<DiaryDay>("/api/diary/2026-10-25?userId=" + self.Id);
        Assert.IsFalse(day!.Entries.Any(x => x.Food.Name == "Private food"));
        Assert.AreEqual(HttpStatusCode.NotFound, (await other.GetAsync("/users")).StatusCode);
    }
    [TestMethod]
    public async Task InvitationsRecoveryAndAdminRightsAreSingleUseAndPrivate()
    {
        string invite;
        await using (var scope = _app.Services.CreateAsyncScope())
        {
            var accounts = scope.ServiceProvider.GetRequiredService<BetaAccounts>();
            await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(() => accounts.IssueAsync(People[1].Id, "intruder@example.com", false));
            invite = await accounts.IssueAsync(People[0].Id, "new-friend@example.com", false);
        }
        await Redeem(invite, Password);
        await Assert.ThrowsExactlyAsync<ValidationException>(() => Redeem(invite, Password));
        string reset;
        await using (var scope = _app.Services.CreateAsyncScope())
        {
            var accounts = scope.ServiceProvider.GetRequiredService<BetaAccounts>();
            reset = await accounts.IssueAsync(People[0].Id, "new-friend@example.com", true);
        }
        await Redeem(reset, "Changed-Test-Only-2026!");
        await Assert.ThrowsExactlyAsync<ValidationException>(() => Redeem(reset, Password));
    }
    [TestMethod]
    public async Task ExpiredLinksCannotBeRedeemed()
    {
        string token;
        await using (var scope = _app.Services.CreateAsyncScope())
        {
            token = await scope.ServiceProvider.GetRequiredService<BetaAccounts>().IssueAsync(People[0].Id, "expired@example.com", false);
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            foreach (var link in db.BetaLinks.Where(x => x.Email == "expired@example.com"))
                link.ExpiresAt = _store.Clock.GetUtcNow().UtcDateTime.AddSeconds(-1);
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsExactlyAsync<ValidationException>(() => Redeem(token, Password));
    }
    private static async Task Redeem(string token, string password)
    {
        await using var scope = _app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<BetaAccounts>().RedeemAsync(token, password);
    }
}
