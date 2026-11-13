using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Data.Sqlite;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Content.Tests.Server.Database;

/// <summary>
/// Guards the donor cape column migration: databases upgraded from PR #396 already recorded these
/// migration identifiers, so a new identifier would try to add <c>selected_donor_cape</c> twice.
/// </summary>
[TestFixture]
public sealed class DonorCapeMigrationTest
{
    private const string SqliteMigration = "20260813000000_DonorCapePreference";
    private const string PostgresMigration = "20260813000001_DonorCapePreference";

    [Test]
    public void MigrationIdentifiersMatchOriginalDonorCapeMigration()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GetDonorCapeMigrationIds(typeof(SqliteServerDbContext)),
                Is.EqualTo(new[] { SqliteMigration }));
            Assert.That(GetDonorCapeMigrationIds(typeof(PostgresServerDbContext)),
                Is.EqualTo(new[] { PostgresMigration }));
        });
    }

    [Test]
    public void OnlyOneMigrationAddsSelectedDonorCapeColumn()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CountAddColumnOperations(typeof(SqliteServerDbContext)), Is.EqualTo(1));
            Assert.That(CountAddColumnOperations(typeof(PostgresServerDbContext)), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task DatabaseMigratedFromOriginalCapeMigrationKeepsSavedSelection()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SqliteServerDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new SqliteServerDbContext(options);

        // This is the database state shipped by PR #396. Later migrations must recognize its
        // original migration ID instead of trying to add selected_donor_cape a second time.
        await db.Database.MigrateAsync(SqliteMigration);

        var preference = new Preference
        {
            UserId = Guid.NewGuid(),
            SelectedCharacterSlot = 0,
            AdminOOCColor = "#ffffff",
        };
        var profile = new Profile
        {
            Slot = 0,
            CharacterName = "Cape migration test",
            FlavorText = string.Empty,
            Age = 25,
            Sex = "Male",
            Gender = "Male",
            Species = "Human",
            HairName = string.Empty,
            HairColor = "#000000",
            FacialHairName = string.Empty,
            FacialHairColor = "#000000",
            EyeColor = "#000000",
            SkinColor = "#000000",
            ArmorPreference = string.Empty,
            SelectedDonorCape = "rmc-donor-cape-16",
        };
        preference.Profiles.Add(profile);
        db.Preference.Add(preference);
        await db.SaveChangesAsync();
        var profileId = profile.Id;

        await db.Database.MigrateAsync();
        db.ChangeTracker.Clear();

        var restored = await db.Profile.AsNoTracking().SingleAsync(existing => existing.Id == profileId);
        Assert.That(restored.SelectedDonorCape, Is.EqualTo("rmc-donor-cape-16"));
    }

    private static string[] GetDonorCapeMigrationIds(Type dbContextType)
    {
        return GetDonorCapeMigrations(dbContextType)
            .Select(migration => migration.GetCustomAttribute<MigrationAttribute>()!.Id)
            .OrderBy(id => id)
            .ToArray();
    }

    private static int CountAddColumnOperations(Type dbContextType)
    {
        return GetDonorCapeMigrations(dbContextType)
            .Select(type => (Migration) Activator.CreateInstance(type)!)
            .SelectMany(migration => migration.UpOperations)
            .OfType<AddColumnOperation>()
            .Count(operation => operation.Table == "profile" && operation.Name == "selected_donor_cape");
    }

    private static IEnumerable<Type> GetDonorCapeMigrations(Type dbContextType)
    {
        return dbContextType.Assembly.GetTypes().Where(type =>
            typeof(Migration).IsAssignableFrom(type) &&
            type.GetCustomAttribute<DbContextAttribute>()?.ContextType == dbContextType &&
            type.GetCustomAttribute<MigrationAttribute>() is { } attribute &&
            attribute.Id.Contains("DonorCapePreference", StringComparison.Ordinal));
    }
}
