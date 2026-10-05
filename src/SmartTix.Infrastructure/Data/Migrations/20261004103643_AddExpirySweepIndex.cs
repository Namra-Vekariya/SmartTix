using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExpirySweepIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_bookings_Status_ExpiresAt",
                table: "bookings",
                columns: new[] { "Status", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_bookings_Status_ExpiresAt",
                table: "bookings");
        }
    }
}
