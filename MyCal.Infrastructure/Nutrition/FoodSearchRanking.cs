using System.Text.RegularExpressions;
using MyCal.Shared.Tracking;

namespace MyCal.Infrastructure.Nutrition;

public static partial class FoodSearchRanking
{
    private static readonly string[] Preparations = ["breaded", "battered", "fried", "deli", "rotisserie", "roll", "glazed", "seasoned"];

    public static IReadOnlyList<FoodItem> Rank(string query, IEnumerable<FoodItem> foods)
    {
        var terms = Words(query);
        return foods.DistinctBy(f => string.IsNullOrWhiteSpace(f.SourceId)
                ? $"{f.Source}:{f.Name}:{f.Brand}:{f.Unit}:{f.Calories}:{f.Protein}:{f.Carbohydrates}:{f.Fat}"
                : $"{f.Source}:{f.SourceId}")
            .OrderByDescending(f => Score(terms, f))
            .ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static int Score(string[] terms, FoodItem food)
    {
        var name = Words(food.Name);
        var brand = Words(food.Brand ?? "");
        var all = name.Concat(brand).ToHashSet();
        var matches = terms.Count(all.Contains);
        var score = terms.Length == 0 ? 0 : matches * 1000 / terms.Length;
        if (terms.SequenceEqual(name))
            score += 100;
        if (brand.Any(terms.Contains))
            score += 40;
        if (food.Source == "USDA FoodData Central" && string.IsNullOrWhiteSpace(food.Brand))
            score += 25;
        // Prefer plain foods for broad queries, without suppressing explicitly requested preparations.
        score -= Preparations.Count(p => name.Contains(p) && !terms.Contains(p)) * 35;
        if (terms.Contains("raw") && !name.Contains("raw"))
            score -= 150;
        if (terms.Contains("cooked") && name.Contains("raw"))
            score -= 150;
        return score - Math.Min(30, name.Length);
    }

    private static string[] Words(string value) => Tokens().Matches(value.ToLowerInvariant()).Select(m => m.Value).ToArray();
    [GeneratedRegex(@"[\p{L}\p{N}]+", RegexOptions.CultureInvariant)]
    private static partial Regex Tokens();
}
