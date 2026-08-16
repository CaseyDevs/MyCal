using MyCal.Domain.Abstractions;
using MyCal.Application.Integrations;
using MyCal.Shared.Dto.Food;

namespace MyCal.Application.Features.Foods;

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
