using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HK_RedHope.Migrations
{
    /// <inheritdoc />
    public partial class AddQueueNumberToDonationHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QueueNumber",
                table: "DonationHistories",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QueueNumber",
                table: "DonationHistories");
        }
    }
}
