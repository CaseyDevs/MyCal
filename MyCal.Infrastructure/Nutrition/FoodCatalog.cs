using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyCal.Domain.Entity;
using MyCal.Application.Calculators;
using MyCal.Shared.Tracking;

namespace MyCal.Infrastructure.Nutrition;

public sealed class FoodCatalog(IHttpClientFactory clients, IMemoryCache cache, IConfiguration configuration,
    TimeProvider clock, ILogger<FoodCatalog> logger) : IFoodSearch
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Queue<DateTimeOffset>> _requests = [];

    // Bounded striped locks coalesce identical searches without retaining one lock per query.
    private readonly SemaphoreSlim[] _searchLocks = Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1)).ToArray();

    public IReadOnlyList<FoodItem> SearchGenericFoods(string query = "") => GenericFoods.Search(query);

    public async Task<FoodSearchPage> SearchAsync(string query, CancellationToken ct = default,
        FoodSearchMarket market = FoodSearchMarket.UnitedKingdom)
    {
        FoodSearchPage page = new([]);
        await foreach (var update in SearchProgressivelyAsync(query, ct, market))
            page = update;
        return page;
    }

    public async IAsyncEnumerable<FoodSearchPage> SearchProgressivelyAsync(string query,
        [EnumeratorCancellation] CancellationToken ct = default, FoodSearchMarket market = FoodSearchMarket.UnitedKingdom)
    {
        query = string.Join(" ", query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (query.Length is < 2 or > 100)
            throw new ValidationException("Search using 2–100 characters.");
        if (!Enum.IsDefined(market))
            throw new ValidationException("Choose UK or worldwide packaged foods.");
        ct.ThrowIfCancellationRequested();
        var foods = SearchGenericFoods(query).ToList();
        if (foods.Count > 0)
            yield return new(foods, IsComplete: false);
        ct.ThrowIfCancellationRequested();
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var pending = new List<Task<FoodSearchPage>>
        {
            SearchProvider("usda", query, market, lifetime.Token),
            SearchProvider("off-search", query, market, lifetime.Token)
        };
        var notices = new List<string>();
        try
        {
            while (pending.Count > 0)
            {
                var next = await Task.WhenAny(pending);
                pending.Remove(next);
                var result = await next;
                ct.ThrowIfCancellationRequested();
                foods.AddRange(result.Foods);
                if (result.Notice is not null)
                    notices.Add(result.Notice);
                yield return new(FoodSearchRanking.Rank(query, foods),
                    notices.Count == 0 ? null : string.Join(" ", notices), pending.Count == 0);
            }
        }
        finally
        {
            // Closing the dialog or starting another query stops both provider requests.
            await lifetime.CancelAsync();
            try
            { await Task.WhenAll(pending); }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        }
    }

    private async Task<FoodSearchPage> SearchProvider(string provider, string query, FoodSearchMarket market, CancellationToken ct)
    {
        var key = $"search:v2:{provider}:{(provider == "usda" ? "generic" : market.ToString())}:{query.ToLowerInvariant()}";
        if (cache.TryGetValue<FoodSearchPage>(key, out var cached))
            return cached!;
        var gate = _searchLocks[((uint)StringComparer.Ordinal.GetHashCode(key) % 32) * 2 + (provider == "usda" ? 0 : 1)];
        await gate.WaitAsync(ct);
        try
        {
            if (cache.TryGetValue(key, out cached))
                return cached!;
            var result = await FetchProvider(provider, query, market, ct);
            cache.Set(key, result, result.Notice is null ? TimeSpan.FromHours(1) : TimeSpan.FromSeconds(20));
            return result;
        }
        finally { gate.Release(); }
    }

    private async Task<FoodSearchPage> FetchProvider(string provider, string query, FoodSearchMarket market, CancellationToken ct)
    {
        var generic = provider == "usda";
        var apiKey = configuration["UsdaFoodData:ApiKey"];
        if (generic && string.IsNullOrWhiteSpace(apiKey))
            return new([], "Add a USDA API key for more online generic foods. Built-in generic foods remain available.");
        if (!Permit(provider, generic ? 25 : 10) || (generic && !Permit("usda-hour", 900, TimeSpan.FromHours(1))))
            return new([], generic ? "Generic search is busy. Try again shortly." :
                "Packaged-food search has reached its minute limit. Use recents, favourites, or a custom food.");
        try
        {
            var url = generic
                ? "https://api.nal.usda.gov/fdc/v1/foods/search?dataType=Foundation,SR%20Legacy,Survey%20(FNDDS)&pageSize=40&query="
                    + Uri.EscapeDataString(query)
                : "https://world.openfoodfacts.org/cgi/search.pl?search_simple=1&action=process&json=1&page_size=30&lc=en&sort_by=unique_scans_n&search_terms="
                    + Uri.EscapeDataString(query)
                    + (market == FoodSearchMarket.UnitedKingdom ? "&tagtype_0=countries&tag_contains_0=contains&tag_0=united-kingdom" : "")
                    + "&fields=code,product_name,product_name_en,brands,nutriments,nutrition_data_per,serving_quantity_unit";
            using var json = await GetJson(url, ct, generic ? apiKey : null);
            if (!json.RootElement.TryGetProperty(generic ? "foods" : "products", out var items) || items.ValueKind != JsonValueKind.Array)
                throw new JsonException("Missing search results.");
            var foods = new List<FoodItem>();
            foreach (var item in items.EnumerateArray())
                if ((generic ? ParseUsda(item) : ParseOff(item)) is { } food)
                    foods.Add(food);
            return new(foods);
        }
        catch (Exception e) when (ProviderFailure(e, ct))
        {
            return new([], generic ? "Generic search is temporarily unavailable. Your saved and custom foods still work." :
                "Packaged-food search is temporarily unavailable. Try again or enter the package values manually.");
        }
    }

    public async Task<FoodItem?> BarcodeAsync(string barcode, CancellationToken ct = default)
    {
        barcode = barcode.Trim();
        if (!ValidBarcode(barcode))
            throw new ValidationException("Enter a valid 8, 12, or 13 digit EAN/UPC barcode, including its check digit.");
        if (cache.TryGetValue<FoodItem>("barcode:" + barcode, out var food))
            return food;
        if (!Permit("off-product", 15))
            throw new ValidationException("Barcode lookup has reached its minute limit. Try again shortly or add a custom food.");
        try
        {
            using var json = await GetJson("https://world.openfoodfacts.org/api/v2/product/" + barcode
                + "?fields=code,product_name,brands,nutriments,nutrition_data_per,serving_quantity_unit", ct);
            if (!json.RootElement.TryGetProperty("product", out var product))
                return null;
            food = ParseOff(product);
            if (food is not null)
                food = food with
                {
                    Barcode = barcode,
                    SourceId = food.SourceId ?? barcode
                };
            if (food is null)
                throw new ValidationException("This product has incomplete nutrition data. Enter its package values as a custom food.");
            cache.Set("barcode:" + barcode, food, TimeSpan.FromHours(12));
            return food;
        }
        catch (HttpRequestException e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound) { return null; }
        catch (Exception e) when (ProviderFailure(e, ct))
        {
            throw new ValidationException("Barcode lookup is unavailable. Try again or enter the package values manually.");
        }
    }

    public static bool ValidBarcode(string code)
    {
        if (code.Length is not (8 or 12 or 13) || code.Any(c => c is < '0' or > '9'))
            return false;
        var sum = 0;
        var weight = 3;
        for (var i = code.Length - 2; i >= 0; i--)
        {
            sum += (code[i] - '0') * weight;
            weight = weight == 3 ? 1 : 3;
        }
        return (10 - sum % 10) % 10 == code[^1] - '0';
    }

    public static FoodItem? ParseUsda(JsonElement food)
    {
        if (!food.TryGetProperty("foodNutrients", out var nutrients))
            return null;
        decimal? Nutrient(int id, string unit) => nutrients.EnumerateArray()
            .Where(n => n.TryGetProperty("nutrientId", out var i) && i.GetInt32() == id &&
                string.Equals(Text(n, "unitName"), unit, StringComparison.OrdinalIgnoreCase))
            .Select(n => Number(n, "value")).FirstOrDefault();
        // Explicit preference, independent of the provider's array ordering.
        var calories = Nutrient(2048, "kcal") ?? Nutrient(2047, "kcal") ?? Nutrient(1008, "kcal");
        if (calories is null)
            return null;
        var totalCarbs = Nutrient(1005, "g");
        var fibre = Nutrient(1079, "g");
        var carbs = totalCarbs is not null && fibre is not null && totalCarbs >= fibre ? totalCarbs - fibre : null;
        var result = new FoodItem(Text(food, "description") ?? "Unnamed food", Text(food, "brandOwner"), "USDA FoodData Central",
            food.TryGetProperty("fdcId", out var id) ? id.ToString() : null, null, NutritionUnit.Grams, 100, calories.Value,
            Nutrient(1003, "g"), carbs, Nutrient(1004, "g"));
        return ValidResult(result);
    }

    public static FoodItem? ParseOff(JsonElement food)
    {
        if (!food.TryGetProperty("nutriments", out var n))
            return null;
        var energy = Number(n, "energy-kcal_100g") ?? (Number(n, "energy-kj_100g") ?? Number(n, "energy_100g")) / 4.184m;
        var name = Text(food, "product_name_en");
        if (string.IsNullOrWhiteSpace(name))
            name = Text(food, "product_name");
        if (energy is null || string.IsNullOrWhiteSpace(name))
            return null;
        var carbs = Number(n, "carbohydrates_100g");
        if (carbs is null && Number(n, "carbohydrates-total_100g") is { } total && Number(n, "fiber_100g") is { } fibre && total >= fibre)
            carbs = total - fibre;
        // OFF's *_100g fields also represent per-100ml labels. Require an explicit
        // package check instead of guessing density or inferring units from the food name.
        var result = new FoodItem(name, Text(food, "brands"), "Open Food Facts", Text(food, "code"),
            Text(food, "code"), Text(food, "serving_quantity_unit") == "ml" ? NutritionUnit.Millilitres : NutritionUnit.Grams,
            100, energy.Value, Number(n, "proteins_100g"), carbs, Number(n, "fat_100g"), RequiresBasisConfirmation: true);
        return ValidResult(result);
    }

    private static FoodItem? ValidResult(FoodItem food)
    {
        food = food with
        {
            Name = food.Name[..Math.Min(200, food.Name.Length)],
            Brand = food.Brand is null ? null : food.Brand[..Math.Min(200, food.Brand.Length)]
        };
        try
        {
            NutritionMath.ValidateFood(food with
            {
                RequiresBasisConfirmation = false
            });
            return food;
        }
        catch (ValidationException) { return null; }
    }

    private async Task<JsonDocument> GetJson(string url, CancellationToken ct, string? apiKey = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (apiKey is not null)
            request.Headers.Add("X-Api-Key", apiKey);
        request.Headers.UserAgent.ParseAdd("MyCal/1.0 (private nutrition tracker)");
        using var response = await clients.CreateClient("nutrition").SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
    }
    private bool Permit(string name, int limit, TimeSpan? window = null)
    {
        lock (_gate)
        {
            if (!_requests.TryGetValue(name, out var queue))
                _requests[name] = queue = new();
            var now = clock.GetUtcNow();
            while (queue.TryPeek(out var at) && now - at >= (window ?? TimeSpan.FromMinutes(1)))
                queue.Dequeue();
            if (queue.Count >= limit)
                return false;
            queue.Enqueue(now);
            return true;
        }
    }
    private bool ProviderFailure(Exception e, CancellationToken ct)
    {
        if (e is OperationCanceledException && ct.IsCancellationRequested)
            return false;
        if (e is not (HttpRequestException or JsonException or TaskCanceledException or InvalidOperationException or FormatException))
            return false;
        logger.LogWarning("Nutrition provider unavailable ({FailureType}).", e.GetType().Name);
        return true;
    }
    private static string? Text(JsonElement e, string key) => e.TryGetProperty(key, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
    private static decimal? Number(JsonElement e, string key)
    {
        if (!e.TryGetProperty(key, out var p))
            return null;
        if (p.ValueKind == JsonValueKind.Number && p.TryGetDecimal(out var n))
            return n >= 0 ? n : null;
        return p.ValueKind == JsonValueKind.String && decimal.TryParse(p.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out n) && n >= 0 ? n : null;
    }
}
