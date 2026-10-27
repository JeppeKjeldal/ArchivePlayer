using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchivePlayer.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentEpisode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentEpisodeFileName",
                table: "UserMediaItems",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentEpisodeFileName",
                table: "UserMediaItems");
        }
    }
}
