using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FashionStore.Migrations;

public partial class AddGuestCartSession : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Carts_UserId", table: "Carts");
        migrationBuilder.AlterColumn<int>(name: "UserId", table: "Carts", type: "int", nullable: true, oldClrType: typeof(int), oldType: "int");
        migrationBuilder.AddColumn<string>(name: "SessionId", table: "Carts", type: "nvarchar(64)", maxLength: 64, nullable: true);
        migrationBuilder.CreateIndex(name: "IX_Carts_SessionId", table: "Carts", column: "SessionId", unique: true, filter: "[SessionId] IS NOT NULL");
        migrationBuilder.CreateIndex(name: "IX_Carts_UserId", table: "Carts", column: "UserId", unique: true, filter: "[UserId] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Carts_SessionId", table: "Carts");
        migrationBuilder.DropIndex(name: "IX_Carts_UserId", table: "Carts");
        migrationBuilder.DropColumn(name: "SessionId", table: "Carts");
        migrationBuilder.AlterColumn<int>(name: "UserId", table: "Carts", type: "int", nullable: false, defaultValue: 1, oldClrType: typeof(int), oldType: "int", oldNullable: true);
        migrationBuilder.CreateIndex(name: "IX_Carts_UserId", table: "Carts", column: "UserId", unique: true);
    }
}
