using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class RepairPendingProfileModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "custom_color",
                table: "profile_loadout",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "custom_entity",
                table: "profile_loadout",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "custom_name",
                table: "profile_loadout",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "custom_color",
                table: "profile_loadout");

            migrationBuilder.DropColumn(
                name: "custom_entity",
                table: "profile_loadout");

            migrationBuilder.DropColumn(
                name: "custom_name",
                table: "profile_loadout");
        }
    }
}
