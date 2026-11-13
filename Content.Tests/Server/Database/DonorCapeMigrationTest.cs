using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Content.Server.Database;
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
