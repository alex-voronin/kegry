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
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DistanceUnits = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_display_settings", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "filters",
                schema: "feed",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RadiusMeters = table.Column<int>(type: "integer", nullable: false),
                    MeetingDeadline = table.Column<int>(type: "integer", nullable: false),
                    AgeFrom = table.Column<int>(type: "integer", nullable: true),
                    AgeTo = table.Column<int>(type: "integer", nullable: true),
                    Gender = table.Column<int>(type: "integer", nullable: true),
                    HobbyIds = table.Column<Guid[]>(type: "uuid[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_filters", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "profiles",
                schema: "feed",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MyUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OtherUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<decimal>(type: "numeric(2,1)", nullable: false),
                    IsBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    IsInMeeting = table.Column<bool>(type: "boolean", nullable: false),
                    MeetingExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SkippedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "unq_feed_profile_active_meeting_per_user",
                schema: "feed",
                table: "profiles",
                column: "MyUserId",
                unique: true,
                filter: "\"IsInMeeting\" = true");

            migrationBuilder.CreateIndex(
                name: "unq_feed_profile_pair",
                schema: "feed",
                table: "profiles",
                columns: new[] { "MyUserId", "OtherUserId" },
                unique: true);
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
                name: "profiles",
                schema: "feed");
        }
    }
}
