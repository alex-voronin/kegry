using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeedModule.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "feed");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "display_settings",
                schema: "feed",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    distance_units = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_display_settings", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "filters",
                schema: "feed",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    radius_meters = table.Column<int>(type: "integer", nullable: false),
                    age_from = table.Column<int>(type: "integer", nullable: true),
                    age_to = table.Column<int>(type: "integer", nullable: true),
                    gender = table.Column<int>(type: "integer", nullable: true),
                    hobby_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filters", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "user_ratings",
                schema: "feed",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewers_count = table.Column<int>(type: "integer", nullable: false),
                    rating = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_ratings", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "user_relations",
                schema: "feed",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    my_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    other_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating = table.Column<decimal>(type: "numeric(2,1)", nullable: true),
                    is_blocked = table.Column<bool>(type: "boolean", nullable: false),
                    is_in_meeting = table.Column<bool>(type: "boolean", nullable: false),
                    should_rate = table.Column<bool>(type: "boolean", nullable: false),
                    meeting_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    skipped_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_relations", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "unq_feed_user_relation_pair",
                schema: "feed",
                table: "user_relations",
                columns: new[] { "my_user_id", "other_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "unq_feed_user_relation_state_active_meeting_per_user",
                schema: "feed",
                table: "user_relations",
                column: "my_user_id",
                unique: true,
                filter: "\"is_in_meeting\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "display_settings",
                schema: "feed");

            migrationBuilder.DropTable(
                name: "filters",
                schema: "feed");

            migrationBuilder.DropTable(
                name: "user_ratings",
                schema: "feed");

            migrationBuilder.DropTable(
                name: "user_relations",
                schema: "feed");
        }
    }
}
