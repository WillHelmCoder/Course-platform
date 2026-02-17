using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pow.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseAllowPurchase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowPurchase",
                table: "Courses",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowPurchase",
                table: "Courses");
        }
    }
}
