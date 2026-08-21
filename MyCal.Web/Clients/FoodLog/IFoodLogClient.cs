using MyCal.Shared.Dto;
using MyCal.Shared.Dto.Food;

namespace MyCal.Web.Clients.FoodLog;

public interface IFoodLogClient
{
    Task<FoodLogResult> GetFoodLogAsync(int id);
    Task AddToFoodLogAsync(FoodLogRequest request);
}