using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace oamswlatifose.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EMAttendanceOtp",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RequestedTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    WorkLocation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BranchId = table.Column<int>(type: "integer", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMAttendanceOtp", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EMBranch",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Address = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    RadiusMeters = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMBranch", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EMEmployees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeID = table.Column<int>(type: "integer", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Position = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EMEmployeesId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMEmployees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EMEmployees_EMEmployees_EMEmployeesId",
                        column: x => x.EMEmployeesId,
                        principalTable: "EMEmployees",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EMOtpUserRequests",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    OTPid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OTP = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMOtpUserRequests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "EMRoleBasedAccessControl",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CanViewEmployees = table.Column<bool>(type: "boolean", nullable: false),
                    CanEditEmployees = table.Column<bool>(type: "boolean", nullable: false),
                    CanDeleteEmployees = table.Column<bool>(type: "boolean", nullable: false),
                    CanViewAttendance = table.Column<bool>(type: "boolean", nullable: false),
                    CanEditAttendance = table.Column<bool>(type: "boolean", nullable: false),
                    CanGenerateReports = table.Column<bool>(type: "boolean", nullable: false),
                    CanManageUsers = table.Column<bool>(type: "boolean", nullable: false),
                    CanManageRoles = table.Column<bool>(type: "boolean", nullable: false),
                    CanAccessAdminPanel = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMRoleBasedAccessControl", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EMAttendance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    AttendanceDate = table.Column<DateTime>(type: "date", nullable: false),
                    TimeIn = table.Column<TimeSpan>(type: "time", nullable: true),
                    TimeOut = table.Column<TimeSpan>(type: "time", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Shift = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    HoursWorked = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    OvertimeHours = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    WorkLocation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BranchId = table.Column<int>(type: "integer", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMAttendance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EMAttendance_EMEmployees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "EMEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EMWorkSchedule",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    GraceMinutes = table.Column<int>(type: "integer", nullable: false),
                    WorkDays = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "EMEmaillogs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Emaillogsid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    OtpUserRequestId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMEmaillogs", x => x.id);
                    table.ForeignKey(
                        name: "FK_EMEmaillogs_EMOtpUserRequests_OtpUserRequestId",
                        column: x => x.OtpUserRequestId,
                        principalTable: "EMOtpUserRequests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EMAuthorizeruser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordSalt = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    EmployeeId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsEmailVerified = table.Column<bool>(type: "boolean", nullable: false),
                    EmailVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastLogin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedLoginAttempts = table.Column<int>(type: "integer", nullable: false),
                    LockoutEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PasswordResetToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PasswordResetTokenExpires = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMAuthorizeruser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EMAuthorizeruser_EMEmployees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "EMEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EMAuthorizeruser_EMRoleBasedAccessControl_RoleId",
                        column: x => x.RoleId,
                        principalTable: "EMRoleBasedAccessControl",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EMAuthLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    UsernameAttempted = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IPAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    WasSuccessful = table.Column<bool>(type: "boolean", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DeviceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Location = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMAuthLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EMAuthLog_EMAuthorizeruser_UserId",
                        column: x => x.UserId,
                        principalTable: "EMAuthorizeruser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EMJWT",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Token = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RefreshToken = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RefreshTokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    RevokedReason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IPAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMJWT", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EMJWT_EMAuthorizeruser_UserId",
                        column: x => x.UserId,
                        principalTable: "EMAuthorizeruser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EMSession",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    SessionToken = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IPAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LoginTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LogoutTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastActivity = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DeviceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Location = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMSession", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EMSession_EMAuthorizeruser_UserId",
                        column: x => x.UserId,
                        principalTable: "EMAuthorizeruser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "EMRoleBasedAccessControl",
                columns: new[] { "Id", "CanAccessAdminPanel", "CanDeleteEmployees", "CanEditAttendance", "CanEditEmployees", "CanGenerateReports", "CanManageRoles", "CanManageUsers", "CanViewAttendance", "CanViewEmployees", "CreatedAt", "Description", "IsActive", "RoleName", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, true, true, true, true, true, true, true, true, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Full system access", true, "Admin", null },
                    { 2, false, false, true, true, true, false, false, true, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "HR — manage schedules, branches and attendance", true, "HR", null },
                    { 3, false, false, false, false, false, false, false, true, false, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Basic user access", true, "User", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_EMAttendance_AttendanceDate",
                table: "EMAttendance",
                column: "AttendanceDate");

            migrationBuilder.CreateIndex(
                name: "IX_EMAttendance_EmployeeId",
                table: "EMAttendance",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EMAttendance_EmployeeId_AttendanceDate",
                table: "EMAttendance",
                columns: new[] { "EmployeeId", "AttendanceDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EMAttendanceOtp_EmployeeId_Purpose_IsUsed",
                table: "EMAttendanceOtp",
                columns: new[] { "EmployeeId", "Purpose", "IsUsed" });

            migrationBuilder.CreateIndex(
                name: "IX_EMAttendanceOtp_ExpiresAt",
                table: "EMAttendanceOtp",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_EMAuthLog_Timestamp",
                table: "EMAuthLog",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_EMAuthLog_UserId",
                table: "EMAuthLog",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EMAuthLog_UsernameAttempted",
                table: "EMAuthLog",
                column: "UsernameAttempted");

            migrationBuilder.CreateIndex(
                name: "IX_EMAuthLog_WasSuccessful",
                table: "EMAuthLog",
                column: "WasSuccessful");

            migrationBuilder.CreateIndex(
                name: "IX_EMAuthorizeruser_Email",
                table: "EMAuthorizeruser",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EMAuthorizeruser_EmployeeId",
                table: "EMAuthorizeruser",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EMAuthorizeruser_PasswordResetToken",
                table: "EMAuthorizeruser",
                column: "PasswordResetToken");

            migrationBuilder.CreateIndex(
                name: "IX_EMAuthorizeruser_RoleId",
                table: "EMAuthorizeruser",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_EMAuthorizeruser_Username",
                table: "EMAuthorizeruser",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EMBranch_IsActive",
                table: "EMBranch",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_EMEmaillogs_OtpUserRequestId",
                table: "EMEmaillogs",
                column: "OtpUserRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_EMEmployees_Email",
                table: "EMEmployees",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_EMEmployees_EMEmployeesId",
                table: "EMEmployees",
                column: "EMEmployeesId");

            migrationBuilder.CreateIndex(
                name: "IX_EMEmployees_EmployeeID",
                table: "EMEmployees",
                column: "EmployeeID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EMJWT_ExpiresAt",
                table: "EMJWT",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_EMJWT_IsRevoked",
                table: "EMJWT",
                column: "IsRevoked");

            migrationBuilder.CreateIndex(
                name: "IX_EMJWT_RefreshToken",
                table: "EMJWT",
                column: "RefreshToken");

            migrationBuilder.CreateIndex(
                name: "IX_EMJWT_Token",
                table: "EMJWT",
                column: "Token");

            migrationBuilder.CreateIndex(
                name: "IX_EMJWT_UserId",
                table: "EMJWT",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EMOtpUserRequests_OTPid",
                table: "EMOtpUserRequests",
                column: "OTPid");

            migrationBuilder.CreateIndex(
                name: "IX_EMRoleBasedAccessControl_RoleName",
                table: "EMRoleBasedAccessControl",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EMSession_ExpiresAt",
                table: "EMSession",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_EMSession_IsActive",
                table: "EMSession",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_EMSession_SessionToken",
                table: "EMSession",
                column: "SessionToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EMSession_UserId",
                table: "EMSession",
                column: "UserId");

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
                name: "EMAttendance");

            migrationBuilder.DropTable(
                name: "EMAttendanceOtp");

            migrationBuilder.DropTable(
                name: "EMAuthLog");

            migrationBuilder.DropTable(
                name: "EMBranch");

            migrationBuilder.DropTable(
                name: "EMEmaillogs");

            migrationBuilder.DropTable(
                name: "EMJWT");

            migrationBuilder.DropTable(
                name: "EMSession");

            migrationBuilder.DropTable(
                name: "EMWorkSchedule");

            migrationBuilder.DropTable(
                name: "EMOtpUserRequests");

            migrationBuilder.DropTable(
                name: "EMAuthorizeruser");

            migrationBuilder.DropTable(
                name: "EMEmployees");

            migrationBuilder.DropTable(
                name: "EMRoleBasedAccessControl");
        }
    }
}
