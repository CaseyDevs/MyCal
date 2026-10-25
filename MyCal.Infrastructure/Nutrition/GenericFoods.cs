using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;
using MyCal.Application.Calculators;
using MyCal.Domain.Entity;
using MyCal.Shared.Tracking;

namespace MyCal.Infrastructure.Nutrition;

public static partial class GenericFoods
{
    private static readonly IReadOnlyList<Entry> Entries = Load();

    public static IReadOnlyList<FoodItem> Search(string query = "")
    {
        if (query.Length > 100)
            throw new ValidationException("Search using up to 100 characters.");
        var terms = Words(query);
        var matches = Entries.Where(entry =>
        {
            var words = Words(entry.Name + " " + entry.Aliases);
            return terms.All(term => words.Any(word => word.StartsWith(term, StringComparison.Ordinal)));
        }).Select(ToFood);
        return terms.Length == 0 ? matches.ToList() : FoodSearchRanking.Rank(query, matches);
    }

    private static FoodItem ToFood(Entry entry)
    {
        // USDA carbohydrate includes fibre; keep unknown values unknown, as in live search.
        var carbs = entry.TotalCarbohydrates is { } total && entry.Fibre is { } fibre && total >= fibre
            ? (decimal?)(total - fibre) : null;
        return new(entry.Name, null, "USDA FoodData Central", entry.FdcId, null, NutritionUnit.Grams,
            100, entry.Calories, entry.Protein, carbs, entry.Fat);
    }

    private static IReadOnlyList<Entry> Load()
    {
        using var stream = typeof(GenericFoods).Assembly.GetManifestResourceStream("MyCal.GenericFoods.json")
            ?? throw new InvalidOperationException("The built-in food catalogue is missing.");
        var snapshot = JsonSerializer.Deserialize<Snapshot>(stream)
            ?? throw new InvalidOperationException("The built-in food catalogue is empty.");
        foreach (var entry in snapshot.Foods)
            NutritionMath.ValidateFood(ToFood(entry));
        return snapshot.Foods;
    }

    private static string[] Words(string value) => Tokens().Matches(value.ToLowerInvariant()).Select(m => m.Value).ToArray();
    [GeneratedRegex(@"[\p{L}\p{N}]+", RegexOptions.CultureInvariant)]
    private static partial Regex Tokens();

    private sealed record Snapshot(IReadOnlyList<Entry> Foods);
    private sealed record Entry(string FdcId, string Name, string Aliases, decimal Calories,
        decimal? Protein, decimal? TotalCarbohydrates, decimal? Fibre, decimal? Fat);
}
