using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameProductTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Products" SET "Type" = 'GroupExcursion' WHERE "Type" = 'Excursion';
                UPDATE "Products" SET "Type" = 'IndividualTransfer' WHERE "Type" = 'Transfer';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Products" SET "Type" = 'Excursion' WHERE "Type" IN ('GroupExcursion', 'IndividualExcursion');
                UPDATE "Products" SET "Type" = 'Transfer' WHERE "Type" IN ('IndividualTransfer', 'GroupTransfer');
                """);
        }
    }
}
