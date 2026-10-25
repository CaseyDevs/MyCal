using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MyCal.Application.Calculators;
using MyCal.Application.Data;
using MyCal.Domain.Entity;
using MyCal.Domain.Enum;
using MyCal.Shared.Tracking;

namespace MyCal.Application.Tracking;

public sealed class ProfileRequiredException() : Exception("Set up your profile first.");

// Identity ids come only from the server's authenticated principal, never from form fields.
public sealed partial class TrackerService(IDbContextFactory<AppDbContext> factory, TimeProvider clock)
{
    public async Task<Profile?> ProfileAsync(string identityId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var u = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.IdentityUserId == identityId, ct);
        return u is null ? null : MapProfile(u);
    }

    public Task<Profile> SaveProfileAsync(string identityId, string email, ProfileInput input, CancellationToken ct = default)
        => Write(identityId, async db =>
        {
            Validator.ValidateObject(input, new ValidationContext(input), true);
            try
            {
                _ = TimeZoneInfo.FindSystemTimeZoneById(input.TimeZoneId);
            }
            catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                throw new ValidationException("Choose a valid IANA time zone, for example Europe/London.");
            }
            var maintenance = NutritionMath.Maintenance(input.Weight, input.Height, input.Age, input.Coefficient, input.Activity);
            var u = await db.Users.SingleOrDefaultAsync(x => x.IdentityUserId == identityId, ct);
            var isNew = u is null;
            u ??= new User { IdentityUserId = identityId, Email = email };
            u.Name = input.Name.Trim();
            u.Age = input.Age;
            u.HeightInCm = (double)input.Height;
            u.WeightInKg = (double)input.Weight;
            u.WeightGoal = (double)input.GoalWeight;
            u.Gender = input.Coefficient;
            u.ActivityLevel = input.Activity;
            u.TimeZoneId = input.TimeZoneId;
            u.MaintenanceCalories = (double)maintenance;
            u.OnboardingStatus = OnboardingStatus.Complete;
            u.GoalType = input.GoalWeight < input.Weight ? GoalType.LoseWeight :
                input.GoalWeight > input.Weight ? GoalType.GainWeight : GoalType.MaintainWeight;
            if (isNew)
                db.Users.Add(u);
            await db.SaveChangesAsync(ct);
            var today = NutritionMath.LocalToday(clock, u.TimeZoneId);
            if (!await db.Targets.AnyAsync(x => x.UserId == u.Id, ct))
            {
                // Keep manual targets available when the starting estimate is outside beta limits.
                try
                {
                    var initial = NutritionMath.StartingTarget(maintenance, input.Weight, u.GoalType);
                    NutritionMath.ValidateTarget(initial);
                    db.Targets.Add(NewTarget(u.Id, today, maintenance, initial));
                }
                catch (ValidationException) { }
                if (!await db.Weights.AnyAsync(x => x.UserId == u.Id && x.Date == today, ct))
                    db.Weights.Add(new WeightEntry { UserId = u.Id, Date = today, Kilograms = input.Weight });
            }
            await db.SaveChangesAsync(ct);
            return MapProfile(u);
        }, ct);

    public async Task<DateOnly> TodayAsync(string identityId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var u = await RequireUser(db, identityId, ct);
        return NutritionMath.LocalToday(clock, u.TimeZoneId);
    }

    public async Task<DiaryDay> DiaryAsync(string identityId, DateOnly date, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var u = await RequireUser(db, identityId, ct);
        var log = await db.FoodLogs.AsNoTracking().Include(x => x.Entries).ThenInclude(x => x.Food)
            .SingleOrDefaultAsync(x => x.UserId == u.Id && x.Date == date, ct);
        return MapDay(date, log, await TargetAt(db, u.Id, date, ct));
    }

    public async Task<DateOnly?> LegacyLogDateAsync(string identityId, int logId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var u = await RequireUser(db, identityId, ct);
        return await db.FoodLogs.Where(x => x.Id == logId && x.UserId == u.Id).Select(x => (DateOnly?)x.Date).SingleOrDefaultAsync(ct);
    }

    public Task<int> AddEntryAsync(string identityId, DateOnly date, Meal meal, decimal amount, FoodItem food,
        CancellationToken ct = default) => Write(identityId, async db =>
        {
            var u = await RequireUser(db, identityId, ct);
            ValidateDate(date, u);
            ValidateMeal(meal);
            _ = NutritionMath.Portion(food, amount);
            var log = await GetOrCreateDay(db, u.Id, date, ct);
            log.Status = DiaryStatus.Incomplete;
            var entry = NewEntry(log, meal, amount, food);
            db.FoodLogEntries.Add(entry);
            await RememberFood(db, u.Id, food, ct);
            await db.SaveChangesAsync(ct);
            return entry.Id;
        }, ct);

    public Task UpdateEntryAsync(string identityId, int entryId, Meal meal, decimal amount, CancellationToken ct = default)
        => Write(identityId, async db =>
        {
            var u = await RequireUser(db, identityId, ct);
            ValidateMeal(meal);
            var e = await db.FoodLogEntries.Include(x => x.FoodLog).Include(x => x.Food)
                .SingleOrDefaultAsync(x => x.Id == entryId && x.FoodLog.UserId == u.Id, ct)
                ?? throw new KeyNotFoundException("Entry not found.");
            _ = NutritionMath.Portion(MapFood(e), amount);
            e.Amount = amount;
            e.Meal = meal;
            e.FoodLog.Status = DiaryStatus.Incomplete;
            await db.SaveChangesAsync(ct);
            return true;
        }, ct);

    public Task DeleteEntryAsync(string identityId, int entryId, CancellationToken ct = default)
        => Write(identityId, async db =>
        {
            var u = await RequireUser(db, identityId, ct);
            var e = await db.FoodLogEntries.Include(x => x.FoodLog)
                .SingleOrDefaultAsync(x => x.Id == entryId && x.FoodLog.UserId == u.Id, ct)
                ?? throw new KeyNotFoundException("Entry not found.");
            e.FoodLog.Status = DiaryStatus.Incomplete;
            db.FoodLogEntries.Remove(e);
            await db.SaveChangesAsync(ct);
            return true;
        }, ct);

    public Task CopyAsync(string identityId, DateOnly source, DateOnly destination, Meal? meal = null, CancellationToken ct = default)
        => Write(identityId, async db =>
        {
            var u = await RequireUser(db, identityId, ct);
            ValidateDate(destination, u);
            if (source == destination)
                throw new ValidationException("Choose a different destination day.");
            if (meal is { } m)
                ValidateMeal(m);
            var entries = await db.FoodLogEntries.AsNoTracking().Include(x => x.Food)
                .Where(x => x.FoodLog.UserId == u.Id && x.FoodLog.Date == source && (meal == null || x.Meal == meal))
                .ToListAsync(ct);
            if (entries.Count == 0)
                throw new ValidationException("There are no entries to copy from that day or meal.");
            var log = await GetOrCreateDay(db, u.Id, destination, ct);
            foreach (var e in entries)
                db.FoodLogEntries.Add(NewEntry(log, e.Meal, EntryAmount(e), MapFood(e)));
            log.Status = DiaryStatus.Incomplete;
            await db.SaveChangesAsync(ct);
            return true;
        }, ct);

    public Task SetStatusAsync(string identityId, DateOnly date, DiaryStatus status, CancellationToken ct = default)
        => Write(identityId, async db =>
        {
            var u = await RequireUser(db, identityId, ct);
            ValidateDate(date, u);
            if (!Enum.IsDefined(status))
                throw new ValidationException("Choose a valid day status.");
            var log = await GetOrCreateDay(db, u.Id, date, ct);
            var hasEntries = await db.FoodLogEntries.AnyAsync(x => x.FoodLogId == log.Id, ct);
            if (status == DiaryStatus.Complete && !hasEntries)
                throw new ValidationException("For an empty day, explicitly confirm zero intake instead.");
            if (status == DiaryStatus.ConfirmedZero && hasEntries)
                throw new ValidationException("A zero-intake day cannot contain food entries.");
            log.Status = status;
            await db.SaveChangesAsync(ct);
            return true;
        }, ct);

    public async Task<IReadOnlyList<DaySummary>> HistoryAsync(string identityId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        if (to < from || to.DayNumber - from.DayNumber > 366)
            throw new ValidationException("Select a range up to one year.");
        await using var db = await factory.CreateDbContextAsync(ct);
        var u = await RequireUser(db, identityId, ct);
        var logs = await db.FoodLogs.AsNoTracking().Include(x => x.Entries).ThenInclude(x => x.Food)
            .Where(x => x.UserId == u.Id && x.Date >= from && x.Date <= to).ToListAsync(ct);
        var targets = await db.Targets.AsNoTracking().Where(x => x.UserId == u.Id && x.EffectiveFrom <= to)
            .OrderByDescending(x => x.EffectiveFrom).ToListAsync(ct);
        return Enumerable.Range(0, to.DayNumber - from.DayNumber + 1).Select(i =>
        {
            var date = from.AddDays(i);
            var d = MapDay(date, logs.SingleOrDefault(x => x.Date == date), targets.FirstOrDefault(x => x.EffectiveFrom <= date));
            return new DaySummary(date, d.Status, d.Totals.Calories, d.Target);
        }).OrderByDescending(x => x.Date).ToList();
    }

    public async Task<IReadOnlyList<FoodItem>> SavedFoodsAsync(string identityId, bool favouritesOnly = false, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var u = await RequireUser(db, identityId, ct);
        var foods = await db.SavedFoods.AsNoTracking().Where(x => x.UserId == u.Id && (!favouritesOnly || x.IsFavourite))
            .OrderByDescending(x => x.LastUsedAt).Take(50).ToListAsync(ct);
        return foods.Select(MapSaved).ToList();
    }

    public Task SaveFoodAsync(string identityId, FoodItem food, bool favourite, CancellationToken ct = default)
        => Write(identityId, async db =>
        {
            var u = await RequireUser(db, identityId, ct);
            NutritionMath.ValidateFood(food);
            var saved = await RememberFood(db, u.Id, food, ct);
            saved.IsFavourite = favourite;
            await db.SaveChangesAsync(ct);
            return true;
        }, ct);

    public async Task<IReadOnlyList<Target>> TargetsAsync(string identityId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var u = await RequireUser(db, identityId, ct);
        return (await db.Targets.AsNoTracking().Where(x => x.UserId == u.Id)
            .OrderByDescending(x => x.EffectiveFrom).ToListAsync(ct)).Select(MapTarget).ToList();
    }

    public Task SaveTargetAsync(string identityId, TargetInput input, CancellationToken ct = default)
        => Write(identityId, async db =>
        {
            var u = await RequireUser(db, identityId, ct);
            NutritionMath.ValidateTarget(input);
            var effective = NutritionMath.LocalToday(clock, u.TimeZoneId);
            if (await db.Targets.AnyAsync(x => x.UserId == u.Id, ct))
                effective = effective.AddDays(1);
            var revision = await db.Targets.SingleOrDefaultAsync(x => x.UserId == u.Id && x.EffectiveFrom == effective, ct);
            if (revision is null)
                db.Targets.Add(NewTarget(u.Id, effective, (decimal)u.MaintenanceCalories, input));
            else
            {
                revision.Calories = input.Calories;
                revision.Protein = input.Protein;
                revision.Carbohydrates = input.Carbohydrates;
                revision.Fat = input.Fat;
                revision.Goal = input.Goal;
                revision.Training = input.Training;
                revision.ProteinPreference = input.ProteinPreference;
                revision.Diet = input.Diet;
                revision.MaintenanceEstimate = (decimal)u.MaintenanceCalories;
            }
            await db.SaveChangesAsync(ct);
            return true;
        }, ct);

    private async Task<T> Write<T>(string identityId, Func<AppDbContext, Task<T>> action, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(identityId))
            throw new UnauthorizedAccessException();
        await using var db = await factory.CreateDbContextAsync(ct);
        // Serialize writes for this account across requests, circuits, and server instances.
        // This also makes "find or create today's diary" safe under concurrent additions.
        if (db.Database.IsNpgsql())
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({identityId}, 0))", ct);
            var result = await action(db);
            await tx.CommitAsync(ct);
            return result;
        }
        return await action(db);
    }

    private static async Task<User> RequireUser(AppDbContext db, string identityId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(identityId))
            throw new UnauthorizedAccessException();
        return await db.Users.SingleOrDefaultAsync(x => x.IdentityUserId == identityId, ct) ?? throw new ProfileRequiredException();
    }
    private void ValidateDate(DateOnly date, User user)
    {
        if (date < new DateOnly(2000, 1, 1) || date > NutritionMath.LocalToday(clock, user.TimeZoneId))
            throw new ValidationException("Choose today or a previous date from the year 2000 onwards.");
    }
    private static void ValidateMeal(Meal meal)
    {
        if (!Enum.IsDefined(meal))
            throw new ValidationException("Choose a valid meal.");
    }
    private static async Task<FoodLog> GetOrCreateDay(AppDbContext db, int userId, DateOnly date, CancellationToken ct)
    {
        var log = await db.FoodLogs.SingleOrDefaultAsync(x => x.UserId == userId && x.Date == date, ct);
        if (log is not null)
            return log;
        log = new FoodLog { UserId = userId, Date = date };
        db.FoodLogs.Add(log);
        await db.SaveChangesAsync(ct);
        return log;
    }
    private async Task<SavedFood> RememberFood(AppDbContext db, int userId, FoodItem food, CancellationToken ct)
    {
        var clean = food with
        {
            SavedId = null,
            IsFavourite = false
        };
        var fingerprint = Hash(JsonSerializer.Serialize(clean));
        var saved = await db.SavedFoods.SingleOrDefaultAsync(x => x.UserId == userId && x.Fingerprint == fingerprint, ct);
        if (saved is null)
        {
            saved = new SavedFood
            {
                UserId = userId,
                Fingerprint = fingerprint,
                Name = food.Name.Trim(),
                Brand = food.Brand,
                Source = food.Source,
                SourceId = food.SourceId,
                Barcode = food.Barcode,
                Unit = food.Unit,
                ReferenceQuantity = food.ReferenceQuantity,
                Calories = food.Calories,
                Protein = food.Protein,
                Carbohydrates = food.Carbohydrates,
                Fat = food.Fat
            };
            db.SavedFoods.Add(saved);
        }
        saved.LastUsedAt = clock.GetUtcNow().UtcDateTime;
        return saved;
    }
    private static FoodLogEntry NewEntry(FoodLog log, Meal meal, decimal amount, FoodItem food) => new()
    {
        FoodLog = log,
        Name = food.Name.Trim(),
        Brand = food.Brand,
        Source = food.Source,
        SourceId = food.SourceId,
        Barcode = food.Barcode,
        Unit = food.Unit,
        ReferenceQuantity = food.ReferenceQuantity,
        Amount = amount,
        Meal = meal,
        CaloriesPerReference = food.Calories,
        ProteinPerReference = food.Protein,
        CarbohydratesPerReference = food.Carbohydrates,
        FatPerReference = food.Fat
    };
    private static decimal EntryAmount(FoodLogEntry e) => e.Name is null ? e.QuantityInGrams : e.Amount;
    private static FoodItem MapFood(FoodLogEntry e) => e.Name is not null
        ? new(e.Name, e.Brand, e.Source, e.SourceId, e.Barcode, e.Unit, e.ReferenceQuantity,
            e.CaloriesPerReference, e.ProteinPerReference, e.CarbohydratesPerReference, e.FatPerReference)
        : new(e.Food?.Name ?? "Legacy food", e.Food?.Brand, "Legacy — nutrients unverified", null, null,
            NutritionUnit.Grams, 100, (decimal)(e.Food?.Calories ?? 0),
            e.Food?.Protein is > 0 ? (decimal)e.Food.Protein : null, null,
            e.Food?.Fats is > 0 ? (decimal)e.Food.Fats : null);
    private static FoodItem MapSaved(SavedFood f) => new(f.Name, f.Brand, f.Source, f.SourceId, f.Barcode,
        f.Unit, f.ReferenceQuantity, f.Calories, f.Protein, f.Carbohydrates, f.Fat, f.Id, f.IsFavourite);
    private static DiaryDay MapDay(DateOnly date, FoodLog? log, TargetRevision? target)
    {
        var entries = log?.Entries.OrderBy(x => x.Id).Select(e =>
        {
            var food = MapFood(e);
            var amount = EntryAmount(e);
            return new DiaryEntry(e.Id, e.Meal, amount, food, NutritionMath.Portion(food, amount));
        }).ToList() ?? [];
        return new(date, log?.Status ?? DiaryStatus.Incomplete, entries, NutritionMath.Totals(entries), target is null ? null : MapTarget(target));
    }
    private static Profile MapProfile(User u) => new(u.Id, u.Name, u.Email, (decimal)u.HeightInCm,
        (decimal)u.WeightInKg, (decimal)u.WeightGoal, u.Age, u.Gender, u.ActivityLevel, u.TimeZoneId);
    private static Target MapTarget(TargetRevision t) => new(t.Id, t.EffectiveFrom, t.Calories, t.Protein,
        t.Carbohydrates, t.Fat, t.MaintenanceEstimate, t.Goal, t.Training, t.ProteinPreference, t.Diet);
    private static TargetRevision NewTarget(int userId, DateOnly date, decimal maintenance, TargetInput t) => new()
    {
        UserId = userId,
        EffectiveFrom = date,
        Calories = t.Calories,
        Protein = t.Protein,
        Carbohydrates = t.Carbohydrates,
        Fat = t.Fat,
        Goal = t.Goal,
        Training = t.Training,
        ProteinPreference = t.ProteinPreference,
        Diet = t.Diet,
        MaintenanceEstimate = maintenance
    };
    private static Task<TargetRevision?> TargetAt(AppDbContext db, int userId, DateOnly date, CancellationToken ct)
        => db.Targets.AsNoTracking().Where(x => x.UserId == userId && x.EffectiveFrom <= date)
            .OrderByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(ct);
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
