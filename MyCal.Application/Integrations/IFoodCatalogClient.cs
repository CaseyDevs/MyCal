using MyCal.Application.Features.Foods;
using MyCal.Shared.Dto.Food;

namespace MyCal.Application.Integrations;

public interface IFoodCatalogClient
{
    Task<IReadOnlyList<FoodSearchResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken);
}
