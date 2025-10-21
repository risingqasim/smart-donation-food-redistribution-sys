using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartDonationSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModelsWithNGOAndNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DonationRequests_AspNetUsers_NGOId",
                table: "DonationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Donations_AspNetUsers_NGOId",
                table: "Donations");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "Donations",
                newName: "FoodType");

            migrationBuilder.AlterColumn<int>(
                name: "NGOId",
                table: "Donations",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickupAddress",
                table: "Donations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "NGOId",
                table: "DonationRequests",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "ResponseMessage",
                table: "DonationRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NGOs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Contact = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NGOs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NGOs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActionUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RelatedEntityId = table.Column<int>(type: "int", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NGOs_UserId",
                table: "NGOs",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Timestamp",
                table: "Notifications",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.AddForeignKey(
                name: "FK_DonationRequests_NGOs_NGOId",
                table: "DonationRequests",
                column: "NGOId",
                principalTable: "NGOs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Donations_NGOs_NGOId",
                table: "Donations",
                column: "NGOId",
                principalTable: "NGOs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DonationRequests_NGOs_NGOId",
                table: "DonationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Donations_NGOs_NGOId",
                table: "Donations");

            migrationBuilder.DropTable(
                name: "NGOs");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropColumn(
                name: "PickupAddress",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "ResponseMessage",
                table: "DonationRequests");

            migrationBuilder.RenameColumn(
                name: "FoodType",
                table: "Donations",
                newName: "Category");

            migrationBuilder.AlterColumn<string>(
                name: "NGOId",
                table: "Donations",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NGOId",
                table: "DonationRequests",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_DonationRequests_AspNetUsers_NGOId",
                table: "DonationRequests",
                column: "NGOId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Donations_AspNetUsers_NGOId",
                table: "Donations",
                column: "NGOId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
