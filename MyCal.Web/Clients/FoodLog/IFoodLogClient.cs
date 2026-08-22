using MyCal.Shared.Dto;
using MyCal.Shared.Dto.FoodLog;

namespace MyCal.Web.Clients.FoodLog;

public interface IFoodLogClient
{
    Task<FoodLogResult> GetFoodLogAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<FoodLogResult> GetTodaysFoodLogAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task AddToFoodLogAsync(
        FoodLogRequest request,
        CancellationToken cancellationToken = default);
}
