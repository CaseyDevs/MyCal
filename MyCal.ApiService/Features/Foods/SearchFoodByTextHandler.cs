using MyCal.ApiService.Abstractions;
using MyCal.ApiService.Integrations;

namespace MyCal.ApiService.Features.Foods;

public sealed class SearchFoodByTextHandler(
    IFoodCatalogClient client
)
    : IQueryHandler<SearchFoodByTextQuery, IReadOnlyList<FoodSearchResult>>
{
    public async Task<IReadOnlyList<FoodSearchResult>> HandleAsync(
        SearchFoodByTextQuery query, 
        CancellationToken cancellationToken)
    {
        var result = await client.SearchAsync(query.FoodName, cancellationToken);

        return result;
    }
}