using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GameApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLeaderboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PlayerScores",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "PlayerScores",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "PresentsCollected",
                table: "PlayerScores",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<float>(
                name: "Range",
                table: "PlayerScores",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "TimeSpent",
                table: "PlayerScores",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PresentsCollected",
                table: "PlayerScores");

            migrationBuilder.DropColumn(
                name: "Range",
                table: "PlayerScores");

            migrationBuilder.DropColumn(
                name: "TimeSpent",
                table: "PlayerScores");

            migrationBuilder.InsertData(
                table: "PlayerScores",
                columns: new[] { "Id", "PlayerName", "Score" },
                values: new object[,]
                {
                    { 1, "Andi", 1200 },
                    { 2, "Nadia", 980 }
                });
        }
    }
}
