using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VenueModule.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVenue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "venue");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "companies",
                schema: "venue",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    avatar_image_s3key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_companies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "venues",
                schema: "venue",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cover_image_s3key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    coordinates = table.Column<Point>(type: "geography(Point, 4326)", nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    website_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    google_maps_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    instagram_url = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    working_hours = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_venues", x => x.id);
                    table.ForeignKey(
                        name: "fk_venues_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "venue",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "venue_promos",
                schema: "venue",
                columns: table => new
                {
                    venue_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    body = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_venue_promos", x => new { x.venue_id, x.id });
                    table.ForeignKey(
                        name: "fk_venue_promos_venues_venue_id",
                        column: x => x.venue_id,
                        principalSchema: "venue",
                        principalTable: "venues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "visits",
                schema: "venue",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    venue_id = table.Column<Guid>(type: "uuid", nullable: false),
                    checked_in_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_visits", x => new { x.user_id, x.venue_id });
                    table.ForeignKey(
                        name: "fk_visits_venues_venue_id",
                        column: x => x.venue_id,
                        principalSchema: "venue",
                        principalTable: "venues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_companies_name",
                schema: "venue",
                table: "companies",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_venues_company_id",
                schema: "venue",
                table: "venues",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_venues_coordinates",
                schema: "venue",
                table: "venues",
                column: "coordinates")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "ix_visits_venue_id",
                schema: "venue",
                table: "visits",
                column: "venue_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "venue_promos",
                schema: "venue");

            migrationBuilder.DropTable(
                name: "visits",
                schema: "venue");

            migrationBuilder.DropTable(
                name: "venues",
                schema: "venue");

            migrationBuilder.DropTable(
                name: "companies",
                schema: "venue");
        }
    }
}
