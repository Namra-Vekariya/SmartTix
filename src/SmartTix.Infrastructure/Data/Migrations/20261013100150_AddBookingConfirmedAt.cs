using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingConfirmedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "confirmed_at",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true);

            // Dev-only backfill: already-confirmed bookings get their last
            // update time as the confirmation time. Cancelled/Refunded stay null.
            migrationBuilder.Sql(
                "UPDATE bookings SET confirmed_at = updated_at WHERE status = 'Confirmed' AND confirmed_at IS NULL;");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_status_confirmed_at",
                table: "bookings",
                columns: new[] { "status", "confirmed_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_bookings_status_confirmed_at",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "confirmed_at",
                table: "bookings");
        }
    }
}
