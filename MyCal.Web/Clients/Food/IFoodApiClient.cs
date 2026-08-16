using MyCal.Shared.Dto.Food;

namespace MyCal.Web.Clients.Food;

public interface IFoodApiClient
{
    Task<List<FoodSearchResult>?> GetFoodsAsync(string term, CancellationToken cancellationToken = default);
}