using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixEventCreatorForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_events_users_CreatedById",
                table: "events");

            migrationBuilder.DropIndex(
                name: "IX_events_CreatedById",
                table: "events");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "events");

            migrationBuilder.CreateIndex(
                name: "IX_events_CreatedByUserId",
                table: "events",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_events_users_CreatedByUserId",
                table: "events",
                column: "CreatedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_events_users_CreatedByUserId",
                table: "events");

            migrationBuilder.DropIndex(
                name: "IX_events_CreatedByUserId",
                table: "events");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "events",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_events_CreatedById",
                table: "events",
                column: "CreatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_events_users_CreatedById",
                table: "events",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
