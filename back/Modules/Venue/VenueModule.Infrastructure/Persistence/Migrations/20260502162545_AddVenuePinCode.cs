using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VenueModule.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVenuePinCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "pin_code",
                schema: "venue",
                table: "venues",
                type: "character varying(4)",
                maxLength: 4,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pin_code",
                schema: "venue",
                table: "venues");
        }
    }
}
