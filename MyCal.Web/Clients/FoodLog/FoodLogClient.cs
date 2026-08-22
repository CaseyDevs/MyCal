using MyCal.Shared.Dto;
using MyCal.Shared.Dto.FoodLog;

namespace MyCal.Web.Clients.FoodLog;

public sealed class FoodLogClient(HttpClient http) : IFoodLogClient
{
    public async Task<FoodLogResult> GetFoodLogAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var response = await http.GetAsync(
            $"/food-logs/{id}",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<FoodLogResult>(
            cancellationToken);

        return result ?? throw new InvalidOperationException("Api returned an empty food log.");
    }

    public async Task<FoodLogResult> GetTodaysFoodLogAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var response = await http.GetAsync(
            $"/food-logs/today/{userId}",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<FoodLogResult>(
            cancellationToken);

        return result ?? throw new InvalidOperationException("Api returned an empty food log.");
    }

    public async Task AddToFoodLogAsync(
        FoodLogRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await http.PostAsJsonAsync(
            "/food-logs",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}
