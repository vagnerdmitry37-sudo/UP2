using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProductAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Attributes",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ProductAttributes",
                table: "OrderItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Attributes",
                table: "Products",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "ProductAttributes",
                table: "OrderItems",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");
        }
    }
}
