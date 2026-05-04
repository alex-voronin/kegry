using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VenueModule.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVenuePinAndCustomDiscounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_pin_check_in_allowed",
                schema: "venue",
                table: "venues",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "pin_code",
                schema: "venue",
                table: "venues",
                type: "character varying(4)",
                maxLength: 4,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "percent",
                schema: "venue",
                table: "discounts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_pin_check_in_allowed",
                schema: "venue",
                table: "venues");

            migrationBuilder.DropColumn(
                name: "pin_code",
                schema: "venue",
                table: "venues");

            migrationBuilder.AlterColumn<int>(
                name: "percent",
                schema: "venue",
                table: "discounts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
