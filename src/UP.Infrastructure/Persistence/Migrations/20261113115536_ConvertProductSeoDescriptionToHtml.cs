using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConvertProductSeoDescriptionToHtml : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SeoDescription",
                table: "Products",
                type: "character varying(20000)",
                maxLength: 20000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(5000)",
                oldMaxLength: 5000);

            migrationBuilder.Sql("""
                UPDATE "Products"
                SET "SeoDescription" = '<p>' || replace(replace(replace(replace(replace(replace(
                    "SeoDescription", '&', '&amp;'), '<', '&lt;'), '>', '&gt;'),
                    E'\r\n', E'\n'), E'\n\n', '</p><p>'), E'\n', '<br>') || '</p>'
                WHERE "SeoDescription" <> '' AND "SeoDescription" NOT LIKE '<%';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Products"
                SET "SeoDescription" = replace(replace(replace(regexp_replace(
                    replace(replace("SeoDescription", '</p><p>', E'\n\n'), '<br>', E'\n'),
                    '<[^>]*>', '', 'g'), '&lt;', '<'), '&gt;', '>'), '&amp;', '&');
                """);

            migrationBuilder.AlterColumn<string>(
                name: "SeoDescription",
                table: "Products",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20000)",
                oldMaxLength: 20000);
        }
    }
}
