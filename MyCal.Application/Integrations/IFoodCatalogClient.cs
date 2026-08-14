using MyCal.Application.Features.Foods;

namespace MyCal.Application.Integrations;

public interface IFoodCatalogClient
{
    Task<IReadOnlyList<FoodSearchResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken);
}
