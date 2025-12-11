using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HK_RedHope.Migrations
{
    /// <inheritdoc />
    public partial class DonationBloodDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DetailDonationBloodId",
                table: "DonationHistories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DonationBloodDetail",
                columns: table => new
                {
                    DonationBloodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DonationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RegistrationDeadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequiredBloodType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiredBloodVolume = table.Column<int>(type: "int", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimeRange = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RegisteredCount = table.Column<int>(type: "int", nullable: false),
                    MaxRegistrations = table.Column<int>(type: "int", nullable: false),
                    SupportGift = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonationBloodDetail", x => x.DonationBloodId);
                    table.ForeignKey(
                        name: "FK_DonationBloodDetail_DonationBloods_DonationId",
                        column: x => x.DonationId,
                        principalTable: "DonationBloods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DonationHistories_DetailDonationBloodId",
                table: "DonationHistories",
                column: "DetailDonationBloodId");

            migrationBuilder.CreateIndex(
                name: "IX_DonationBloodDetail_DonationId",
                table: "DonationBloodDetail",
                column: "DonationId");

            migrationBuilder.AddForeignKey(
                name: "FK_DonationHistories_DonationBloodDetail_DetailDonationBloodId",
                table: "DonationHistories",
                column: "DetailDonationBloodId",
                principalTable: "DonationBloodDetail",
                principalColumn: "DonationBloodId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DonationHistories_DonationBloodDetail_DetailDonationBloodId",
                table: "DonationHistories");

            migrationBuilder.DropTable(
                name: "DonationBloodDetail");

            migrationBuilder.DropIndex(
                name: "IX_DonationHistories_DetailDonationBloodId",
                table: "DonationHistories");

            migrationBuilder.DropColumn(
                name: "DetailDonationBloodId",
                table: "DonationHistories");
        }
    }
}
