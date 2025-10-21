using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartDonationSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "NGOs",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "NGOs",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "AspNetUsers",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "AspNetUsers",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "NGOs");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "NGOs");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "AspNetUsers");
        }
    }
}
