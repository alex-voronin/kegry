using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngagementModule.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEngagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "engagement");

            migrationBuilder.CreateTable(
                name: "npcs",
                schema: "engagement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    avatar_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    monolog_key = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_npcs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "engagement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    seen_npc_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_met_npcs",
                schema: "engagement",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    npc_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_met_npcs", x => new { x.user_id, x.npc_id });
                    table.ForeignKey(
                        name: "fk_user_met_npcs_npcs_npc_id",
                        column: x => x.npc_id,
                        principalSchema: "engagement",
                        principalTable: "npcs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_met_npcs_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "engagement",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "engagement",
                table: "npcs",
                columns: new[] { "id", "avatar_url", "monolog_key", "name_key" },
                values: new object[] { new Guid("d6d4f3a8-d445-e700-eacd-5e022bcbe3f6"), "assets/npc/gobby-snugs.jpg", "GobbySnugs.Monolog", "GobbySnugs.Name" });

            migrationBuilder.CreateIndex(
                name: "ix_user_met_npcs_npc_id",
                schema: "engagement",
                table: "user_met_npcs",
                column: "npc_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_met_npcs",
                schema: "engagement");

            migrationBuilder.DropTable(
                name: "npcs",
                schema: "engagement");

            migrationBuilder.DropTable(
                name: "users",
                schema: "engagement");
        }
    }
}
