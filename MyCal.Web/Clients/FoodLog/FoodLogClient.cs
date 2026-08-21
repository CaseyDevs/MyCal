using MyCal.Shared.Dto;
using MyCal.Shared.Dto.Food;

namespace MyCal.Web.Clients.FoodLog;

public sealed class FoodLogClient(HttpClient http) : IFoodLogClient
{
    public async Task<FoodLogResult> GetFoodLogAsync(int id)
    {
        var response = await http.GetAsync($"food-logs/{id}");
        
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<FoodLogResult>();
        return result ?? throw new InvalidOperationException("Api returned an empty food log.");
    }

    public async Task AddToFoodLogAsync(FoodLogRequest request)
    {
        var response = await http.PostAsJsonAsync("food-logs", request);
        response.EnsureSuccessStatusCode();
    }
}