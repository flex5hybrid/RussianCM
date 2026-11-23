using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Sqlite;

[DbContext(typeof(SqliteServerDbContext))]
[Migration("20261123000000_RuMCModerationOutbox")]
public sealed class RuMCModerationOutbox : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "rumc_moderation_outbox",
            columns: table => new
            {
                id = table.Column<Guid>(type: "TEXT", nullable: false),
                webhook_id = table.Column<string>(type: "TEXT", nullable: false),
                payload = table.Column<string>(type: "TEXT", nullable: false),
                available_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                failed_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                lease_id = table.Column<Guid>(type: "TEXT", nullable: true),
            },
            constraints: table => table.PrimaryKey("PK_rumc_moderation_outbox", row => row.id));

        migrationBuilder.CreateIndex(
            name: "IX_rumc_moderation_outbox_webhook_id_available_at",
            table: "rumc_moderation_outbox",
            columns: new[] { "webhook_id", "available_at" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("rumc_moderation_outbox");
    }
}
