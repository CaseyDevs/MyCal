using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyCal.Infrastructure;

namespace MyCal.Tests;

[TestClass, DoNotParallelize]
public sealed class MigrationTests
{
    [TestMethod]
    public async Task DietUpgradeKeepsExistingTargetDatesAndAmounts()
    {
        await using var store = await TestStore.CreateAsync(migrate: false);
        await using var db = await store.Factory.CreateDbContextAsync();
        await db.GetService<IMigrator>().MigrateAsync("20261025105336_TrackingBeta");
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO "Users" ("Id","IdentityUserId","Name","Email","HeightInCm","WeightInKg","WeightGoal","Age","Gender","ActivityLevel","MaintenanceCalories","GoalType","OnboardingStatus","CreatedAt")
            VALUES (9002,'existing-diet','Existing','existing@example.com',180,75,70,30,1,1,2500,1,2,NOW());
            INSERT INTO "Targets" ("UserId","EffectiveFrom","Calories","Protein","Carbohydrates","Fat","MaintenanceEstimate","Goal","CreatedAt")
            VALUES (9002,'2026-10-24',2000,140,180,80,2500,1,NOW()),
                   (9002,'2026-10-25',2100,150,240,60,2500,1,NOW());
            """);
        await db.Database.MigrateAsync();
        var yesterday = (await store.Tracker.DiaryAsync("existing-diet", new(2026, 10, 24))).Target!;
        var today = (await store.Tracker.DiaryAsync("existing-diet", new(2026, 10, 25))).Target!;
        Assert.AreEqual(2000m, yesterday.Calories);
        Assert.AreEqual(140m, yesterday.Protein);
        Assert.AreEqual(180m, yesterday.Carbohydrates);
        Assert.AreEqual(80m, yesterday.Fat);
        Assert.AreEqual(2100m, today.Calories);
        Assert.AreEqual(150m, today.Protein);
        Assert.AreEqual(240m, today.Carbohydrates);
        Assert.AreEqual(60m, today.Fat);
        Assert.AreEqual(MyCal.Domain.Entity.DietPreference.Custom, today.Diet);
    }

    [TestMethod]
    public async Task UpgradePreservesAccountsPasskeysAndFreezesOldNutrition()
    {
        await using var store = await TestStore.CreateAsync(migrate: false);
        await using var db = await store.Factory.CreateDbContextAsync();
        await db.GetService<IMigrator>().MigrateAsync("20260820223407_AddFoodLogEntry");
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO "Users" ("Id","IdentityUserId","Name","Email","HeightInCm","WeightInKg","WeightGoal","Age","Gender","ActivityLevel","MaintenanceCalories","GoalType","OnboardingStatus","CreatedAt")
            VALUES (9001,'legacy-id','Legacy','legacy@example.com',180,75,70,30,1,1,2076,1,2,NOW());
            INSERT INTO "Food" ("Id","Name","Calories","Protein","Carbohydrates","Fats","CreatedAt") VALUES (9001,'Original oats',380,13,68,7,NOW());
            INSERT INTO "FoodLog" ("Id","UserId","Date","TotalCalories","CreatedAt") VALUES (9001,9001,'2026-08-20',190,NOW());
            INSERT INTO "FoodLogEntries" ("Id","FoodLogId","FoodId","QuantityInGrams","CreatedAt") VALUES (9001,9001,9001,50,NOW());
            """);
        await db.Database.MigrateAsync();
        var before = await store.Tracker.DiaryAsync("legacy-id", new(2026, 8, 20));
        Assert.AreEqual(190m, before.Totals.Calories);
        Assert.IsNull(before.Target);
        Assert.IsTrue(before.Totals.CarbohydratesIncomplete);
        await db.Database.ExecuteSqlRawAsync("UPDATE \"Food\" SET \"Calories\" = 900 WHERE \"Id\" = 9001");
        Assert.AreEqual(190m, (await store.Tracker.DiaryAsync("legacy-id", new(2026, 8, 20))).Totals.Calories);
        Assert.AreEqual("Europe/London", (await store.Tracker.ProfileAsync("legacy-id"))!.TimeZoneId);
        await using var identity = DesignTimeIdentityContext.Create(store.IdentityConnection);
        await identity.GetService<IMigrator>().MigrateAsync("20260719195457_InitialIdentity");
        await identity.Database.ExecuteSqlRawAsync("""
            INSERT INTO "AspNetUsers" ("Id","UserName","NormalizedUserName","Email","NormalizedEmail","EmailConfirmed","PasswordHash","SecurityStamp","ConcurrencyStamp","PhoneNumberConfirmed","TwoFactorEnabled","LockoutEnabled","AccessFailedCount")
            VALUES ('legacy-id','legacy@example.com','LEGACY@EXAMPLE.COM','legacy@example.com','LEGACY@EXAMPLE.COM',true,'unchanged-hash','stamp','stamp',false,false,true,0);
            INSERT INTO "AspNetUserPasskeys" ("CredentialId","UserId","Data") VALUES (decode('010203','hex'),'legacy-id','{{}}'::jsonb);
            """);
        await identity.Database.MigrateAsync();
        Assert.AreEqual("unchanged-hash", (await identity.Users.SingleAsync(x => x.Id == "legacy-id")).PasswordHash);
        Assert.AreEqual(1, await identity.UserPasskeys.CountAsync());
    }
}
