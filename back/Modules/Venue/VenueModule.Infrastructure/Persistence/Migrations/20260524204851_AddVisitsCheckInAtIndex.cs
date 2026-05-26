using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VenueModule.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitsCheckInAtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_visits_venue_id",
                schema: "venue",
                table: "visits");

            migrationBuilder.CreateIndex(
                name: "ix_visits_venue_id_checked_in_at",
                schema: "venue",
                table: "visits",
                columns: new[] { "venue_id", "checked_in_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_visits_venue_id_checked_in_at",
                schema: "venue",
                table: "visits");

            migrationBuilder.CreateIndex(
                name: "ix_visits_venue_id",
                schema: "venue",
                table: "visits",
                column: "venue_id");
        }
    }
}
