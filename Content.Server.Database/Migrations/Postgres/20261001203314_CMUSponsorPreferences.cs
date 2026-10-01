using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class CMUSponsorPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "approved",
                table: "rmc_patron_lobby_messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Preserve already published lobby messages; future edits reset approval.
            migrationBuilder.Sql("UPDATE rmc_patron_lobby_messages SET approved = TRUE;");

            migrationBuilder.CreateTable(
                name: "cmu_sponsor_preferences",
                columns: table => new
                {
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    settings = table.Column<string>(type: "text", nullable: false),
                    approved_figurine_description = table.Column<string>(type: "text", nullable: false),
                    custom_item = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cmu_sponsor_preferences", x => x.player_id);
                    table.ForeignKey(
                        name: "FK_cmu_sponsor_preferences_player_player_id1",
                        column: x => x.player_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cmu_sponsor_preferences");

            migrationBuilder.DropColumn(
                name: "approved",
                table: "rmc_patron_lobby_messages");
        }
    }
}
