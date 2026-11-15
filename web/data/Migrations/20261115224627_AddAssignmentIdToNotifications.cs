using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web.data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentIdToNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignmentId",
                table: "Notifications",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignmentId",
                table: "Notifications");
        }
    }
}
