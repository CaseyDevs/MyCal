using MyCal.Shared.Dto.Food;

namespace MyCal.Web.Clients.Food;

public interface IFoodApiClient
{
    Task<IReadOnlyList<FoodSearchResult>> GetFoodsAsync(string term, CancellationToken cancellationToken = default);
}