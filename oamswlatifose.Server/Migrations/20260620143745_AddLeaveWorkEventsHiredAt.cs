using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace oamswlatifose.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveWorkEventsHiredAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "HiredAt",
                table: "EMEmployees",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EMLeaveRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "date", nullable: false),
                    EndDate = table.Column<DateTime>(type: "date", nullable: false),
                    LeaveType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "integer", nullable: true),
                    ApprovalNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMLeaveRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EMLeaveRequests_EMEmployees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "EMEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EMWorkEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    EventType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMWorkEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EMLeaveRequests_EmployeeId",
                table: "EMLeaveRequests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EMLeaveRequests_Status",
                table: "EMLeaveRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EMWorkEvents_Date",
                table: "EMWorkEvents",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_EMWorkEvents_EventType",
                table: "EMWorkEvents",
                column: "EventType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EMLeaveRequests");

            migrationBuilder.DropTable(
                name: "EMWorkEvents");

            migrationBuilder.DropColumn(
                name: "HiredAt",
                table: "EMEmployees");
        }
    }
}
