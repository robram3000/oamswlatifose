using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace oamswlatifose.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchIdToEmployees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "EMEmployees",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EMEmployees_BranchId",
                table: "EMEmployees",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_EMEmployees_EMBranch_BranchId",
                table: "EMEmployees",
                column: "BranchId",
                principalTable: "EMBranch",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EMEmployees_EMBranch_BranchId",
                table: "EMEmployees");

            migrationBuilder.DropIndex(
                name: "IX_EMEmployees_BranchId",
                table: "EMEmployees");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "EMEmployees");
        }
    }
}
