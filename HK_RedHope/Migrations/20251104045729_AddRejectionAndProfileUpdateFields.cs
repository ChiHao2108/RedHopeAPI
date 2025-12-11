using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HK_RedHope.Migrations
{
    /// <inheritdoc />
    public partial class AddRejectionAndProfileUpdateFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRejected",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRejected",
                table: "AspNetUsers");
        }
    }
}
