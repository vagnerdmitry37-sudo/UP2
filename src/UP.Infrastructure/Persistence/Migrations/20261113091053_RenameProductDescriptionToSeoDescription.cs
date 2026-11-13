using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameProductDescriptionToSeoDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Products",
                newName: "SeoDescription");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SeoDescription",
                table: "Products",
                newName: "Description");
        }
    }
}
