using System.Net;
using MyCal.Shared.Dto.Food;

namespace MyCal.Web.Clients.Food;

/// <summary>
/// Client for the Food API, currently only USDA.
/// </summary>
/// <param name="http"></param>
public class FoodApiClient(HttpClient http) : IFoodApiClient
{
    public async Task<IReadOnlyList<FoodSearchResult>> GetFoodsAsync(string term, CancellationToken cancellationToken = default!)
    {
        var response = await http.GetAsync(
            $"/foods?search={term}", cancellationToken);
        
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<FoodSearchResult>>(cancellationToken);
        return result ?? [];
    }
}