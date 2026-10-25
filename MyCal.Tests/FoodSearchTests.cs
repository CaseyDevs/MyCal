using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MyCal.Domain.Entity;
using MyCal.Infrastructure.Nutrition;
using MyCal.Shared.Tracking;

namespace MyCal.Tests;

[TestClass]
public class FoodSearchTests
{
    [TestMethod]
    public void PlainChickenRanksAboveBreadedButExplicitPreparationWins()
    {
        var foods = new[] { Food("Chicken breast tenders, breaded, uncooked", "1"),
            Food("Chicken, breast, boneless, skinless, raw", "2"),
            Food("Chicken, breast, boneless, skinless, cooked, roasted", "3") };
        var plain = FoodSearchRanking.Rank("chicken breast", foods);
        Assert.AreEqual("2", plain[0].SourceId);
        Assert.AreEqual("3", plain[1].SourceId);
        Assert.AreEqual("1", FoodSearchRanking.Rank("breaded chicken breast", foods)[0].SourceId);
        Assert.AreEqual("3", FoodSearchRanking.Rank("cooked chicken breast", foods)[0].SourceId);
    }

    [TestMethod]
    public void BrandMatchesWinAndOnlyIdenticalSourceIdsAreDeduplicated()
    {
        var branded = Food("Chicken breast", "uk") with { Source = "Open Food Facts", Brand = "Tesco" };
        var generic = Food("Chicken breast", "raw");
        var cooked = Food("Chicken breast cooked", "cooked");
        var ranked = FoodSearchRanking.Rank("Tesco chicken breast", [generic, branded, cooked, branded]);
        Assert.AreEqual(branded, ranked[0]);
        Assert.AreEqual(3, ranked.Count);
    }

    [TestMethod]
    public async Task FastSourceIsDeliveredWhileTheOtherIsStillRunning()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var http = new Handler(async (request, ct) =>
        {
            if (request.RequestUri!.Host == "world.openfoodfacts.org")
            {
                started.SetResult();
                await release.Task.WaitAsync(ct);
                return Json(Off);
            }
            return Json(Usda);
        });
        var catalog = Catalog(http);
        await using var stream = catalog.SearchProgressivelyAsync("chicken breast").GetAsyncEnumerator();
        Assert.IsTrue(await stream.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(3)));
        await started.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.IsFalse(stream.Current.IsComplete);
        Assert.AreEqual("Chicken breast raw", stream.Current.Foods.Single().Name);
        release.SetResult();
        Assert.IsTrue(await stream.MoveNextAsync());
        Assert.IsTrue(stream.Current.IsComplete);
        Assert.AreEqual(2, stream.Current.Foods.Count);
    }

    [TestMethod]
    public async Task UkSearchIncludesCountryFilterAndWorldwideUsesSeparateCache()
    {
        var urls = new List<string>();
        using var http = new Handler((request, _) =>
        {
            lock (urls)
                urls.Add(request.RequestUri!.AbsoluteUri);
            return Task.FromResult(Json(request.RequestUri!.Host == "world.openfoodfacts.org" ? Off : Usda));
        });
        var catalog = Catalog(http);
        await catalog.SearchAsync("chicken breast");
        await catalog.SearchAsync(" CHICKEN   BREAST ");
        Assert.AreEqual(2, urls.Count);
        var uk = urls.Single(u => u.Contains("openfoodfacts"));
        StringAssert.Contains(uk, "tagtype_0=countries");
        StringAssert.Contains(uk, "tag_0=united-kingdom");
        await catalog.SearchAsync("chicken breast", market: FoodSearchMarket.Worldwide);
        Assert.AreEqual(3, urls.Count, "Generic results should be reused across markets.");
        Assert.IsFalse(urls.Last().Contains("tag_0="));
    }

    [TestMethod]
    public async Task ConcurrentIdenticalSearchesShareProviderRequests()
    {
        var count = 0;
        using var http = new Handler(async (request, ct) =>
        {
            Interlocked.Increment(ref count);
            await Task.Delay(40, ct);
            return Json(request.RequestUri!.Host == "world.openfoodfacts.org" ? Off : Usda);
        });
        var catalog = Catalog(http);
        var results = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => catalog.SearchAsync("chicken breast")));
        Assert.AreEqual(2, count);
        Assert.IsTrue(results.All(r => r.Foods.Count == 2 && r.IsComplete));
    }

    [TestMethod]
    public async Task ProviderFailurePreservesOtherResultsAndReportsPartialCoverage()
    {
        using var http = new Handler((request, _) => Task.FromResult(request.RequestUri!.Host == "world.openfoodfacts.org"
            ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) : Json(Usda)));
        var page = await Catalog(http).SearchAsync("chicken breast");
        Assert.AreEqual(1, page.Foods.Count);
        StringAssert.Contains(page.Notice!, "Packaged-food search is temporarily unavailable");
    }

    [TestMethod]
    public async Task CancellingSearchStopsBothRequests()
    {
        var count = 0;
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopped = 0;
        using var http = new Handler(async (_, ct) =>
        {
            if (Interlocked.Increment(ref count) == 2)
                started.SetResult();
            try
            { await Task.Delay(Timeout.InfiniteTimeSpan, ct); }
            finally { Interlocked.Increment(ref stopped); }
            return Json(Usda);
        });
        using var cancellation = new CancellationTokenSource();
        var search = Catalog(http).SearchAsync("chicken breast", cancellation.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(3));
        cancellation.Cancel();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => search);
        Assert.AreEqual(2, stopped);
    }

    [TestMethod]
    public async Task PackagedSearchEnforcesItsProviderRateLimit()
    {
        var calls = 0;
        using var http = new Handler((request, _) =>
        {
            if (request.RequestUri!.Host == "world.openfoodfacts.org")
                Interlocked.Increment(ref calls);
            return Task.FromResult(Json(request.RequestUri!.Host == "world.openfoodfacts.org" ? Off : Usda));
        });
        var catalog = Catalog(http);
        FoodSearchPage? page = null;
        for (var i = 0; i < 11; i++)
            page = await catalog.SearchAsync("food " + i);
        Assert.AreEqual(10, calls);
        StringAssert.Contains(page!.Notice!, "minute limit");
    }

    private static FoodItem Food(string name, string id) => new(name, null, "USDA FoodData Central", id,
        null, NutritionUnit.Grams, 100, 120, 20, 0, 4);
    private const string Usda = """{"foods":[{"fdcId":1,"description":"Chicken breast raw","foodNutrients":[{"nutrientId":1008,"unitName":"KCAL","value":120}]}]}""";
    private const string Off = """{"products":[{"code":"uk1","product_name":"Poulet","product_name_en":"Chicken breast","brands":"Tesco","nutriments":{"energy-kcal_100g":120}}]}""";
    private static HttpResponseMessage Json(string text) => new(HttpStatusCode.OK) { Content = new StringContent(text, Encoding.UTF8, "application/json") };
    private static FoodCatalog Catalog(HttpMessageHandler handler) => new(new ClientFactory(handler),
        new MemoryCache(new MemoryCacheOptions()), new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["UsdaFoodData:ApiKey"] = "test-key" }).Build(), TimeProvider.System,
        NullLogger<FoodCatalog>.Instance);
    private sealed class ClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
    }
}
