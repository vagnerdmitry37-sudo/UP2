using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConvertProductSeoDescriptionToPlainText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                WITH plain AS (
                    SELECT "Id", btrim(regexp_replace(
                        replace(replace(replace(replace(replace(replace(replace(
                            regexp_replace("SeoDescription", '<[^>]*>', ' ', 'g'),
                            '&nbsp;', ' '), '&lt;', '<'), '&gt;', '>'), '&quot;', '"'),
                            '&#39;', ''''), '&#x27;', ''''), '&amp;', '&'),
                        '\s+', ' ', 'g')) AS text
                    FROM "Products"
                )
                UPDATE "Products" AS product
                SET "SeoDescription" = CASE
                    WHEN length(plain.text) <= 160 THEN plain.text
                    ELSE rtrim(coalesce(substring(left(plain.text, 159) from '^(.*)\s'), left(plain.text, 159)), ' .,;:!?-') || '…'
                END
                FROM plain
                WHERE product."Id" = plain."Id";
                """);

            migrationBuilder.AlterColumn<string>(
                name: "SeoDescription",
                table: "Products",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20000)",
                oldMaxLength: 20000);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SeoDescription",
                table: "Products",
                type: "character varying(20000)",
                maxLength: 20000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(160)",
                oldMaxLength: 160);

            migrationBuilder.Sql("""
                UPDATE "Products"
                SET "SeoDescription" = '<p>' || replace(replace(replace(
                    "SeoDescription", '&', '&amp;'), '<', '&lt;'), '>', '&gt;') || '</p>'
                WHERE "SeoDescription" <> '';
                """);
        }
    }
}
