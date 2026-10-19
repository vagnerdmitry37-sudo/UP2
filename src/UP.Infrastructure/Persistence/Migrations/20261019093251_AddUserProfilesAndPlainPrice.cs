using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfilesAndPlainPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceCurrency",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "PriceAmount",
                table: "Products",
                newName: "Price");

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProfiles_AspNetUsers_Id",
                        column: x => x.Id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Products",
                newName: "PriceAmount");

            migrationBuilder.AddColumn<string>(
                name: "PriceCurrency",
                table: "Products",
                type: "character(3)",
                fixedLength: true,
                maxLength: 3,
                nullable: false,
                defaultValue: "");
        }
    }
}
