using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace oamswlatifose.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchGeofenceAndLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "EMAttendanceOtp",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "EMAttendanceOtp",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "EMAttendanceOtp",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkLocation",
                table: "EMAttendanceOtp",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "EMAttendance",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "EMAttendance",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "EMAttendance",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkLocation",
                table: "EMAttendance",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EMBranch",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    RadiusMeters = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMBranch", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "EMRoleBasedAccessControl",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 19, 14, 50, 20, 784, DateTimeKind.Utc).AddTicks(4817));

            migrationBuilder.UpdateData(
                table: "EMRoleBasedAccessControl",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 19, 14, 50, 20, 784, DateTimeKind.Utc).AddTicks(4820));

            migrationBuilder.UpdateData(
                table: "EMRoleBasedAccessControl",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 19, 14, 50, 20, 784, DateTimeKind.Utc).AddTicks(4822));

            migrationBuilder.CreateIndex(
                name: "IX_EMBranch_IsActive",
                table: "EMBranch",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EMBranch");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "EMAttendanceOtp");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "EMAttendanceOtp");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "EMAttendanceOtp");

            migrationBuilder.DropColumn(
                name: "WorkLocation",
                table: "EMAttendanceOtp");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "EMAttendance");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "EMAttendance");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "EMAttendance");

            migrationBuilder.DropColumn(
                name: "WorkLocation",
                table: "EMAttendance");

            migrationBuilder.UpdateData(
                table: "EMRoleBasedAccessControl",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 19, 10, 37, 48, 470, DateTimeKind.Utc).AddTicks(6036));

            migrationBuilder.UpdateData(
                table: "EMRoleBasedAccessControl",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 19, 10, 37, 48, 470, DateTimeKind.Utc).AddTicks(6039));

            migrationBuilder.UpdateData(
                table: "EMRoleBasedAccessControl",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 19, 10, 37, 48, 470, DateTimeKind.Utc).AddTicks(6042));
        }
    }
}
