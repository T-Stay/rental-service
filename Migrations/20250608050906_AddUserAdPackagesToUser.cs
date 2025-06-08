using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalService.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAdPackagesToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "UserAdPackages",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_UserAdPackages_UserId1",
                table: "UserAdPackages",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_UserAdPackages_AspNetUsers_UserId1",
                table: "UserAdPackages",
                column: "UserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserAdPackages_AspNetUsers_UserId1",
                table: "UserAdPackages");

            migrationBuilder.DropIndex(
                name: "IX_UserAdPackages_UserId1",
                table: "UserAdPackages");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "UserAdPackages");
        }
    }
}
