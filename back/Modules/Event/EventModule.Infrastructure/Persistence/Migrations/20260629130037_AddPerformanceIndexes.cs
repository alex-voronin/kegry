using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventModule.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_events_creator_id_start_date",
                schema: "event",
                table: "events",
                columns: new[] { "creator_id", "start_date_time" });

            migrationBuilder.CreateIndex(
                name: "ix_events_start_date",
                schema: "event",
                table: "events",
                column: "start_date_time");

            migrationBuilder.CreateIndex(
                name: "ix_events_venue_id_start_date",
                schema: "event",
                table: "events",
                columns: new[] { "venue_id", "start_date_time" });

            migrationBuilder.CreateIndex(
                name: "ix_event_participants_user_id_event_id",
                schema: "event",
                table: "event_participants",
                columns: new[] { "user_id", "event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_participants_user_id_status",
                schema: "event",
                table: "event_participants",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_event_hobby_links_hobby_id_event_id",
                schema: "event",
                table: "event_hobby_links",
                columns: new[] { "hobby_id", "event_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_events_creator_id_start_date",
                schema: "event",
                table: "events");

            migrationBuilder.DropIndex(
                name: "ix_events_start_date",
                schema: "event",
                table: "events");

            migrationBuilder.DropIndex(
                name: "ix_events_venue_id_start_date",
                schema: "event",
                table: "events");

            migrationBuilder.DropIndex(
                name: "ix_event_participants_user_id_event_id",
                schema: "event",
                table: "event_participants");

            migrationBuilder.DropIndex(
                name: "ix_event_participants_user_id_status",
                schema: "event",
                table: "event_participants");

            migrationBuilder.DropIndex(
                name: "ix_event_hobby_links_hobby_id_event_id",
                schema: "event",
                table: "event_hobby_links");
        }
    }
}
