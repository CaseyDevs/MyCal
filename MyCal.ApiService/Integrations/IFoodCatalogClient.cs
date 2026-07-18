using MyCal.ApiService.Features.Foods;

namespace MyCal.ApiService.Integrations;

public interface IFoodCatalogClient
{
    Task<IReadOnlyList<FoodSearchResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken);
}
