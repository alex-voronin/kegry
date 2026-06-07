using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventModule.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventAvatar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "creator_avatar_s3key",
                schema: "event",
                table: "events",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "creator_avatar_s3key",
                schema: "event",
                table: "events");
        }
    }
}
