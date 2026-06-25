using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationsModule.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLinkedLinkedEntityId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "linked_entity_id",
                schema: "notifications",
                table: "notifications",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "linked_entity_id",
                schema: "notifications",
                table: "notifications");
        }
    }
}
