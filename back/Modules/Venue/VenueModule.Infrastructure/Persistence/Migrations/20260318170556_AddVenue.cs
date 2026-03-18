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
                    avatar_image_s3key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_companies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "loyalty_levels",
                schema: "venue",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    visits_upper_bound = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loyalty_levels", x => new { x.id, x.company_id });
                    table.CheckConstraint("CK_loyalty_levels_id", "\"id\" IN (0, 1, 2, 3)");
                    table.ForeignKey(
                        name: "fk_loyalty_levels_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "venue",
                        principalTable: "companies",
                        principalColumn: "id");
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
                    time_zone_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    website_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    google_maps_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    instagram_url = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    working_hours = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_venues", x => x.id);
                    table.ForeignKey(
                        name: "fk_venues_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "venue",
                        principalTable: "companies",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "user_loyalties",
                schema: "venue",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    visits_count = table.Column<int>(type: "integer", nullable: false),
                    loyalty_level_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_loyalties", x => new { x.user_id, x.company_id });
                    table.ForeignKey(
                        name: "fk_user_loyalties_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "venue",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_loyalties_loyalty_levels_loyalty_level_id_company_id",
                        columns: x => new { x.loyalty_level_id, x.company_id },
                        principalSchema: "venue",
                        principalTable: "loyalty_levels",
                        principalColumns: new[] { "id", "company_id" });
                });

            migrationBuilder.CreateTable(
                name: "discounts",
                schema: "venue",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    venue_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    loyalty_level_id = table.Column<int>(type: "integer", nullable: false),
                    percent = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_discounts_loyalty_levels_loyalty_level_id_company_id",
                        columns: x => new { x.loyalty_level_id, x.company_id },
                        principalSchema: "venue",
                        principalTable: "loyalty_levels",
                        principalColumns: new[] { "id", "company_id" });
                    table.ForeignKey(
                        name: "fk_discounts_venues_venue_id",
                        column: x => x.venue_id,
                        principalSchema: "venue",
                        principalTable: "venues",
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
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    venue_id = table.Column<Guid>(type: "uuid", nullable: false),
                    checked_in_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_visits", x => x.id);
                    table.ForeignKey(
                        name: "fk_visits_venues_venue_id",
                        column: x => x.venue_id,
                        principalSchema: "venue",
                        principalTable: "venues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "discount_days",
                schema: "venue",
                columns: table => new
                {
                    discount_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discount_days", x => new { x.discount_id, x.day_of_week });
                    table.CheckConstraint("CK_discount_days_day_of_week", "\"day_of_week\" IN (0, 1, 2, 3, 4, 5, 6)");
                    table.ForeignKey(
                        name: "fk_discount_days_discounts_discount_id",
                        column: x => x.discount_id,
                        principalSchema: "venue",
                        principalTable: "discounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_companies_is_deleted",
                schema: "venue",
                table: "companies",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_companies_owner_id",
                schema: "venue",
                table: "companies",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_discounts_loyalty_level_id_company_id",
                schema: "venue",
                table: "discounts",
                columns: new[] { "loyalty_level_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "ix_discounts_venue_id",
                schema: "venue",
                table: "discounts",
                column: "venue_id");

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_levels_company_id",
                schema: "venue",
                table: "loyalty_levels",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_loyalties_company_id",
                schema: "venue",
                table: "user_loyalties",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_loyalties_loyalty_level_id_company_id",
                schema: "venue",
                table: "user_loyalties",
                columns: new[] { "loyalty_level_id", "company_id" });

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
                name: "ix_venues_is_deleted",
                schema: "venue",
                table: "venues",
                column: "is_deleted");

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
                name: "discount_days",
                schema: "venue");

            migrationBuilder.DropTable(
                name: "user_loyalties",
                schema: "venue");

            migrationBuilder.DropTable(
                name: "venue_promos",
                schema: "venue");

            migrationBuilder.DropTable(
                name: "visits",
                schema: "venue");

            migrationBuilder.DropTable(
                name: "discounts",
                schema: "venue");

            migrationBuilder.DropTable(
                name: "loyalty_levels",
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
