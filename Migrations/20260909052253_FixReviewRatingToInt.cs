using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FashionStore.Migrations
{
    /// <inheritdoc />
    public partial class FixReviewRatingToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop check constraint if exists
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_Reviews_Rating') ALTER TABLE [Reviews] DROP CONSTRAINT CK_Reviews_Rating;");
            
            // Fix Rating column type from TINYINT to INT
            migrationBuilder.AlterColumn<int>(
                name: "Rating",
                table: "Reviews",
                type: "int",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");
            
            // Re-add check constraint for Rating between 1 and 5
            migrationBuilder.Sql("ALTER TABLE [Reviews] ADD CONSTRAINT CK_Reviews_Rating CHECK ([Rating] >= 1 AND [Rating] <= 5);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert Rating column type from INT to TINYINT
            migrationBuilder.AlterColumn<byte>(
                name: "Rating",
                table: "Reviews",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
