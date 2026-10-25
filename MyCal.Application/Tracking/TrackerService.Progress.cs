using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MyCal.Application.Calculators;
using MyCal.Application.Data;
using MyCal.Domain.Entity;
using MyCal.Shared.Tracking;

namespace MyCal.Application.Tracking;

public sealed partial class TrackerService
{
    public Task SaveWeightAsync(string identityId, DateOnly date, decimal kg, CancellationToken ct = default)
        => Write(identityId, async db =>
        {
            var u = await RequireUser(db, identityId, ct);
            ValidateDate(date, u);
            if (kg is < 30 or > 350)
                throw new ValidationException("Weight must be between 30 and 350 kg.");
            var w = await db.Weights.SingleOrDefaultAsync(x => x.UserId == u.Id && x.Date == date, ct);
            if (w is null)
                db.Weights.Add(new WeightEntry { UserId = u.Id, Date = date, Kilograms = kg });
            else
                w.Kilograms = kg;
            await db.SaveChangesAsync(ct);
            return true;
        }, ct);

    public Task DeleteWeightAsync(string identityId, DateOnly date, CancellationToken ct = default)
        => Write(identityId, async db =>
        {
            var u = await RequireUser(db, identityId, ct);
            var weight = await db.Weights.SingleOrDefaultAsync(x => x.UserId == u.Id && x.Date == date, ct)
                ?? throw new KeyNotFoundException("Weight entry not found.");
            db.Weights.Remove(weight);
            await db.SaveChangesAsync(ct);
            return true;
        }, ct);

    public async Task<ProgressData> ProgressAsync(string identityId, DateOnly through, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var u = await RequireUser(db, identityId, ct);
        var from = through.AddDays(-89);
        var weights = await db.Weights.AsNoTracking().Where(x => x.UserId == u.Id && x.Date >= from && x.Date <= through)
            .OrderBy(x => x.Date).Select(x => new WeightPoint(x.Date, x.Kilograms)).ToListAsync(ct);
        var logs = await db.FoodLogs.AsNoTracking().Include(x => x.Entries).ThenInclude(x => x.Food)
            .Where(x => x.UserId == u.Id && x.Date >= through.AddDays(-28) && x.Date < through && x.Status != DiaryStatus.Incomplete)
            .ToListAsync(ct);
        var totals = logs.Select(x => MapDay(x.Date, x, null).Totals).ToList();
        return new(weights, totals.Count, totals.Count == 0 ? null : totals.Average(x => x.Calories),
            totals.Count == 0 || totals.Any(x => x.ProteinIncomplete) ? null : totals.Average(x => x.Protein),
            totals.Count == 0 || totals.Any(x => x.CarbohydratesIncomplete) ? null : totals.Average(x => x.Carbohydrates),
            totals.Count == 0 || totals.Any(x => x.FatIncomplete) ? null : totals.Average(x => x.Fat));
    }

    public Task<ReviewResult> ReviewAsync(string identityId, CancellationToken ct = default)
        => Write(identityId, async db =>
        {
            var u = await RequireUser(db, identityId, ct);
            var review = await BuildReview(db, u, ct);
            var existing = await db.Reviews.SingleOrDefaultAsync(x => x.UserId == u.Id && x.Date == review.Date && x.Fingerprint == review.Fingerprint, ct);
            if (existing is not null)
                return MapReview(existing);
            db.Reviews.Add(review);
            await db.SaveChangesAsync(ct);
            return MapReview(review);
        }, ct);

    public Task AcceptReviewAsync(string identityId, int reviewId, CancellationToken ct = default)
        => Write(identityId, async db =>
        {
            var u = await RequireUser(db, identityId, ct);
            var review = await db.Reviews.SingleOrDefaultAsync(x => x.Id == reviewId && x.UserId == u.Id, ct)
                ?? throw new KeyNotFoundException("Review not found.");
            if (review.AcceptedAt is not null)
                throw new ValidationException("This suggestion has already been accepted.");
            var current = await BuildReview(db, u, ct);
            if (review.Date != current.Date || review.Fingerprint != current.Fingerprint || review.SuggestedCalories is null ||
                current.SuggestedCalories != review.SuggestedCalories)
                throw new ValidationException("Your data or target has changed. Run a fresh review before accepting.");
            var target = await db.Targets.SingleAsync(x => x.Id == review.TargetRevisionId && x.UserId == u.Id, ct);
            var calories = review.SuggestedCalories.Value;
            var input = NutritionMath.AdjustCalories(MapTarget(target), calories);
            db.Targets.Add(NewTarget(u.Id, review.Date.AddDays(1), target.MaintenanceEstimate, input));
            review.AcceptedAt = clock.GetUtcNow().UtcDateTime;
            await db.SaveChangesAsync(ct);
            return true;
        }, ct);

    public async Task<IReadOnlyList<ReviewResult>> ReviewHistoryAsync(string identityId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var u = await RequireUser(db, identityId, ct);
        return (await db.Reviews.AsNoTracking().Where(x => x.UserId == u.Id).OrderByDescending(x => x.Id)
            .Take(20).ToListAsync(ct)).Select(MapReview).ToList();
    }

    private async Task<CoachingReview> BuildReview(AppDbContext db, User u, CancellationToken ct)
    {
        var today = NutritionMath.LocalToday(clock, u.TimeZoneId);
        var revision = await TargetAt(db, u.Id, today, ct) ?? throw new ValidationException("Save your starting targets first.");
        var logs = await db.FoodLogs.AsNoTracking().Include(x => x.Entries).ThenInclude(x => x.Food)
            .Where(x => x.UserId == u.Id && x.Date >= today.AddDays(-28) && x.Date < today)
            .OrderBy(x => x.Date).ToListAsync(ct);
        var weights = await db.Weights.AsNoTracking().Where(x => x.UserId == u.Id && x.Date >= today.AddDays(-28) && x.Date < today)
            .OrderBy(x => x.Date).Select(x => new WeightPoint(x.Date, x.Kilograms)).ToListAsync(ct);
        var scheduled = await db.Targets.AnyAsync(x => x.UserId == u.Id && x.EffectiveFrom > today, ct);
        var days = logs.Select(x => MapDay(x.Date, x, null)).ToList();
        var decision = WeeklyCoach.Evaluate(today, MapTarget(revision), days, weights, scheduled);
        // Fingerprint inputs so an old suggestion cannot be accepted after a diary correction.
        var fingerprint = Hash(JsonSerializer.Serialize(new
        {
            today,
            target = MapTarget(revision),
            days,
            weights,
            scheduled
        }));
        return new CoachingReview
        {
            UserId = u.Id,
            Date = today,
            TargetRevisionId = revision.Id,
            Fingerprint = fingerprint,
            Reason = decision.Reason,
            CurrentCalories = revision.Calories,
            SuggestedCalories = decision.SuggestedCalories,
            WeeklyLossPercent = decision.WeeklyLossPercent
        };
    }
    private static ReviewResult MapReview(CoachingReview r) => new(r.Id, r.Reason, r.CurrentCalories,
        r.SuggestedCalories, r.WeeklyLossPercent, r.AcceptedAt is not null);
}
