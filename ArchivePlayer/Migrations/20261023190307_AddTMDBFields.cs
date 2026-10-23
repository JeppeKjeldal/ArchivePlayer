using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchivePlayer.Migrations
{
    /// <inheritdoc />
    public partial class AddTMDBFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TMDBId",
                table: "MediaItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TMDBMediaType",
                table: "MediaItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TMDBPosterUrl",
                table: "MediaItems",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TMDBId",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "TMDBMediaType",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "TMDBPosterUrl",
                table: "MediaItems");
        }
    }
}
