using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalService.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneOtpAndPhoneVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "ContactInformations",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PhoneOtps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Otp = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExpiredAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastSentAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsUsed = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneOtps", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ContactInformations_Data",
                table: "ContactInformations",
                column: "Data",
                unique: true,
                filter: "[Type] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneOtps_PhoneNumber",
                table: "PhoneOtps",
                column: "PhoneNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhoneOtps");

            migrationBuilder.DropIndex(
                name: "IX_ContactInformations_Data",
                table: "ContactInformations");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "ContactInformations");
        }
    }
}
