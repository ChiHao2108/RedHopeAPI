using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HK_RedHope.Migrations
{
    /// <inheritdoc />
    public partial class AddDonationBloodResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DonationBloodResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DonationHistoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsEligible = table.Column<bool>(type: "bit", nullable: false),
                    BloodType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MedicalHistory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentHealthStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RiskBehavior = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPregnant = table.Column<bool>(type: "bit", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonationBloodResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonationBloodResults_DonationHistories_DonationHistoryId",
                        column: x => x.DonationHistoryId,
                        principalTable: "DonationHistories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DonationBloodResults_DonationHistoryId",
                table: "DonationBloodResults",
                column: "DonationHistoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DonationBloodResults");
        }
    }
}
