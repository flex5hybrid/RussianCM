using System.Linq;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.IntegrationTests._RuCM.Database;

[TestFixture]
public sealed class ProfileLoadoutMigrationTests
{
    [TestCase(true)]
    [TestCase(false)]
    public void CurrentModelMatchesSnapshotAndMigrationOnlyAddsOptionalLoadoutFields(bool postgres)
    {
        using DbContext db = postgres
            ? new PostgresServerDbContext(new DbContextOptionsBuilder<PostgresServerDbContext>().UseNpgsql("Host=localhost;Database=unused_model_check").Options)
            : new SqliteServerDbContext(new DbContextOptionsBuilder<SqliteServerDbContext>().UseSqlite("Data Source=:memory:").Options);
        Assert.That(db.Database.HasPendingModelChanges(), Is.False);
        var migrations = db.GetService<IMigrationsAssembly>();
        var migration = migrations.CreateMigration(migrations.Migrations.Last().Value, db.Database.ProviderName!);
        Assert.That(migration.GetType().Name, Is.EqualTo("RepairPendingProfileModel"));
        Assert.That(migration.UpOperations, Has.Count.EqualTo(3));
        Assert.That(migration.UpOperations, Has.All.TypeOf<Microsoft.EntityFrameworkCore.Migrations.Operations.AddColumnOperation>());
        var columns = migration.UpOperations.Cast<Microsoft.EntityFrameworkCore.Migrations.Operations.AddColumnOperation>().ToArray();
        Assert.That(columns.Select(c => c.Name), Is.EquivalentTo(new[] { "custom_color", "custom_entity", "custom_name" }));
        Assert.That(columns.All(c => c.Table == "profile_loadout" && c.IsNullable), Is.True);
        var sql = db.GetService<IMigrator>().GenerateScript(migrations.Migrations.Keys.SkipLast(1).Last(), migrations.Migrations.Keys.Last());
        Assert.That(sql, Does.Contain("ADD"));
        Assert.That(sql, Does.Not.Contain("DROP TABLE"));
    }
}
