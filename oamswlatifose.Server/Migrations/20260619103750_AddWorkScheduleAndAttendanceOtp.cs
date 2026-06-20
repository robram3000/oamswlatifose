using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace oamswlatifose.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkScheduleAndAttendanceOtp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EMAttendanceOtp",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequestedTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMAttendanceOtp", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EMWorkSchedule",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    GraceMinutes = table.Column<int>(type: "int", nullable: false),
                    WorkDays = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMWorkSchedule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EMWorkSchedule_EMEmployees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "EMEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_EMAttendanceOtp_EmployeeId_Purpose_IsUsed",
                table: "EMAttendanceOtp",
                columns: new[] { "EmployeeId", "Purpose", "IsUsed" });

            migrationBuilder.CreateIndex(
                name: "IX_EMAttendanceOtp_ExpiresAt",
                table: "EMAttendanceOtp",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_EMWorkSchedule_EmployeeId",
                table: "EMWorkSchedule",
                column: "EmployeeId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EMAttendanceOtp");

            migrationBuilder.DropTable(
                name: "EMWorkSchedule");

            migrationBuilder.UpdateData(
                table: "EMRoleBasedAccessControl",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 25, 12, 36, 19, 495, DateTimeKind.Utc).AddTicks(9408));

            migrationBuilder.UpdateData(
                table: "EMRoleBasedAccessControl",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 25, 12, 36, 19, 495, DateTimeKind.Utc).AddTicks(9412));

            migrationBuilder.UpdateData(
                table: "EMRoleBasedAccessControl",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 25, 12, 36, 19, 495, DateTimeKind.Utc).AddTicks(9414));
        }
    }
}
