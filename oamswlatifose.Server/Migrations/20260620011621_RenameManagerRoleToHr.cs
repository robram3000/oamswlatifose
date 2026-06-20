using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace oamswlatifose.Server.Migrations
{
    /// <inheritdoc />
    public partial class RenameManagerRoleToHr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "EMRoleBasedAccessControl",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 20, 1, 16, 21, 3, DateTimeKind.Utc).AddTicks(2893));

            migrationBuilder.UpdateData(
                table: "EMRoleBasedAccessControl",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "Description", "RoleName" },
                values: new object[] { new DateTime(2026, 6, 20, 1, 16, 21, 3, DateTimeKind.Utc).AddTicks(2897), "HR — manage schedules, branches and attendance", "HR" });

            migrationBuilder.UpdateData(
                table: "EMRoleBasedAccessControl",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 20, 1, 16, 21, 3, DateTimeKind.Utc).AddTicks(2899));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "EMRoleBasedAccessControl",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 20, 0, 55, 39, 231, DateTimeKind.Utc).AddTicks(2667));

            migrationBuilder.UpdateData(
                table: "EMRoleBasedAccessControl",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "Description", "RoleName" },
                values: new object[] { new DateTime(2026, 6, 20, 0, 55, 39, 231, DateTimeKind.Utc).AddTicks(2671), "Manager level access", "Manager" });

            migrationBuilder.UpdateData(
                table: "EMRoleBasedAccessControl",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 20, 0, 55, 39, 231, DateTimeKind.Utc).AddTicks(2673));
        }
    }
}
