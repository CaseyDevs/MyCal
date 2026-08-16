using System.Net;
using MyCal.Shared.Dto.Food;

namespace MyCal.Web.Clients.Food;

/// <summary>
/// Client for the Food API, currently only USDA.
/// </summary>
/// <param name="http"></param>
public class FoodApiClient(HttpClient http) : IFoodApiClient
{
    public async Task<List<FoodSearchResult>?> GetFoodsAsync(string term, CancellationToken cancellationToken = default!)
    {
        var response = await http.GetAsync(
            $"/foods?search={term}", cancellationToken);
        
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<List<FoodSearchResult>>(cancellationToken);
    }
}