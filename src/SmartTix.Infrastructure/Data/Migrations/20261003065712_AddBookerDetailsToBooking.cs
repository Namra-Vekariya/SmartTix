using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTix.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookerDetailsToBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BookerEmail",
                table: "bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BookerName",
                table: "bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BookerPhone",
                table: "bookings",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BookerEmail",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "BookerName",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "BookerPhone",
                table: "bookings");
        }
    }
}
