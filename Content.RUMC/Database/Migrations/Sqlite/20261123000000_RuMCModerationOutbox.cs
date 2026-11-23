using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Sqlite;

[DbContext(typeof(SqliteServerDbContext))]
[Migration("20261123000000_CMUModerationOutbox")]
public sealed class CMUModerationOutbox : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "cmu_moderation_outbox",
            columns: table => new
            {
                id = table.Column<Guid>(type: "TEXT", nullable: false),
                webhook_id = table.Column<string>(type: "TEXT", nullable: false),
                payload = table.Column<string>(type: "TEXT", nullable: false),
                available_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                lease_id = table.Column<Guid>(type: "TEXT", nullable: true),
            },
            constraints: table => table.PrimaryKey("PK_cmu_moderation_outbox", row => row.id));

        migrationBuilder.CreateIndex(
            name: "IX_cmu_moderation_outbox_webhook_id_available_at",
            table: "cmu_moderation_outbox",
            columns: new[] { "webhook_id", "available_at" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("cmu_moderation_outbox");
    }
}
