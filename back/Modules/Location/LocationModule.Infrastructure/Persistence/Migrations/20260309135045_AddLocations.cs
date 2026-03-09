using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace LocationModule.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "location");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "locations",
                schema: "location",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coordinates = table.Column<Point>(type: "geometry(Point, 4326)", nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_locations", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "tracked_users",
                schema: "location",
                columns: table => new
                {
                    first_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    second_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tracked_users", x => new { x.first_user_id, x.second_user_id });
                });

            migrationBuilder.CreateIndex(
                name: "ix_locations_coordinates",
                schema: "location",
                table: "locations",
                column: "coordinates")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "ix_locations_userid_covering",
                schema: "location",
                table: "locations",
                column: "user_id")
                .Annotation("Npgsql:IndexInclude", new[] { "coordinates" });

            migrationBuilder.CreateIndex(
                name: "ix_tracked_users_expires_at",
                schema: "location",
                table: "tracked_users",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_tracked_users_first_user_id",
                schema: "location",
                table: "tracked_users",
                column: "first_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_tracked_users_second_user_id",
                schema: "location",
                table: "tracked_users",
                column: "second_user_id");
            
            // Don't delete! Important index for good feed performance.
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_locations_geog
                ON location.locations USING GIST ((coordinates::geography));
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "locations",
                schema: "location");

            migrationBuilder.DropTable(
                name: "tracked_users",
                schema: "location");
        }
    }
}
