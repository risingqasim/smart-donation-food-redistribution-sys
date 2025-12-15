using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartDonationSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddPredictionEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Predictions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonationId = table.Column<int>(type: "int", nullable: true),
                    NGOId = table.Column<int>(type: "int", nullable: true),
                    PredictionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PredictedValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    MatchScore = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    DemandScore = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    DistanceKm = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAccurate = table.Column<bool>(type: "bit", nullable: true),
                    ActualOutcome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OutcomeRecordedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Predictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Predictions_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Predictions_Donations_DonationId",
                        column: x => x.DonationId,
                        principalTable: "Donations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Predictions_NGOs_NGOId",
                        column: x => x.NGOId,
                        principalTable: "NGOs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_CreatedAt",
                table: "Predictions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_CreatedByUserId",
                table: "Predictions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_DonationId_PredictionType",
                table: "Predictions",
                columns: new[] { "DonationId", "PredictionType" });

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_IsAccurate",
                table: "Predictions",
                column: "IsAccurate");

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_NGOId_PredictionType",
                table: "Predictions",
                columns: new[] { "NGOId", "PredictionType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Predictions");
        }
    }
}
