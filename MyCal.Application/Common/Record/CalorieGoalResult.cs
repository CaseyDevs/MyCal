namespace MyCal.Application.Common.Record;

public sealed record CalorieGoalResult
(
    double MaintenanceCalories,
    double DailyAdjustment,
    double TargetCalories
);