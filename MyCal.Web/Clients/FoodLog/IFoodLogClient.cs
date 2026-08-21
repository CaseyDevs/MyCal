using MyCal.Shared.Dto;
using MyCal.Shared.Dto.FoodLog;

namespace MyCal.Web.Clients.FoodLog;

public interface IFoodLogClient
{
    Task<FoodLogResult> GetFoodLogAsync(int id);
    Task AddToFoodLogAsync(FoodLogRequest request);
}
