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
        db.Preference.Add(preference);
        await db.SaveChangesAsync();

        // Insert using columns present in the historical schema. The current EF model includes
        // fields added by later migrations, so SaveChanges would target columns that do not exist yet.
        const int slot = 0;
        const int age = 25;
        const int unavailable = 0;
        const string characterName = "Cape migration test";
        const string sex = "Male";
        const string empty = "";
        const string black = "#000000";
        const string selectedCape = "rmc-donor-cape-16";
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO profile (
                slot, char_name, age, sex, hair_name, hair_color, facial_hair_name,
                facial_hair_color, eye_color, skin_color, pref_unavailable, preference_id,
                selected_donor_cape)
            VALUES (
                {slot}, {characterName}, {age}, {sex}, {empty}, {black}, {empty},
                {black}, {black}, {black}, {unavailable}, {preference.Id}, {selectedCape})
            """);

        await db.Database.MigrateAsync();
        db.ChangeTracker.Clear();

        var restored = await db.Profile.AsNoTracking().SingleAsync(existing => existing.CharacterName == characterName);
        Assert.That(restored.SelectedDonorCape, Is.EqualTo(selectedCape));
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
