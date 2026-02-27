using Microsoft.EntityFrameworkCore;
using oamswlatifose.Server.Model.security;
using oamswlatifose.Server.Model.smtp;
using oamswlatifose.Server.Model.occurance;
using oamswlatifose.Server.Model.user;

namespace oamswlatifose.Server.Model
{
    public class ApplicationDbContext : DbContext
    {
        // Security entities
        public DbSet<EMAuthorizeruser> EMAuthorizerusers { get; set; }
        public DbSet<EMAuthLog> EMAuthLogs { get; set; }
        public DbSet<EMJWT> EMJWT { get; set; }
        public DbSet<EMRoleBasedAccessControl> EMRoleBasedAccessControls { get; set; }
        public DbSet<EMSession> EMSessions { get; set; }

        // User entities
        public DbSet<EMEmployees> EMEmployees { get; set; }

        // Occurrence entities
        public DbSet<EMAttendance> EMAttendance { get; set; }

        // SMTP entities
        public DbSet<EMEmaillogs> EMEmaillogs { get; set; }
        public DbSet<EMOtpUserRequest> EMOtpUserRequests { get; set; }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // EMAuthorizeruser Configuration
            modelBuilder.Entity<EMAuthorizeruser>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.Username)
                    .IsUnique();

                entity.HasIndex(e => e.Email)
                    .IsUnique();

                entity.HasIndex(e => e.PasswordResetToken);

                // Relationships
                entity.HasOne(e => e.Role)
                    .WithMany(r => r.Users)
                    .HasForeignKey(e => e.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Employee)
                    .WithOne(e => e.UserAccount)
                    .HasForeignKey<EMAuthorizeruser>(e => e.EmployeeId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(e => e.Sessions)
                    .WithOne(s => s.User)
                    .HasForeignKey(s => s.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.AuthLogs)
                    .WithOne(a => a.User)
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // EMAuthLog Configuration
            modelBuilder.Entity<EMAuthLog>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.UsernameAttempted);
                entity.HasIndex(e => e.WasSuccessful);

                entity.Property(e => e.IPAddress)
                    .HasMaxLength(45);
            });

            // EMJWT Configuration
            modelBuilder.Entity<EMJWT>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.Token);
                entity.HasIndex(e => e.RefreshToken);
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasIndex(e => e.IsRevoked);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // EMRoleBasedAccessControl Configuration
            modelBuilder.Entity<EMRoleBasedAccessControl>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.RoleName)
                    .IsUnique();

                entity.Property(e => e.RoleName)
                    .IsRequired()
                    .HasMaxLength(100);
            });

            // EMSession Configuration
            modelBuilder.Entity<EMSession>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.SessionToken)
                    .IsUnique();

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasIndex(e => e.IsActive);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Sessions)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // EMEmployees Configuration
            modelBuilder.Entity<EMEmployees>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.EmployeeID)
                    .IsUnique();

                entity.HasIndex(e => e.Email);

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(255);

                // Relationships
              entity.HasOne(e => e.UserAccount)
                    .WithOne(u => u.Employee)
                    .HasForeignKey<EMAuthorizeruser>(u => u.EmployeeId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // EMAttendance Configuration
            modelBuilder.Entity<EMAttendance>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.EmployeeId);
                entity.HasIndex(e => e.AttendanceDate);
                entity.HasIndex(e => new { e.EmployeeId, e.AttendanceDate })
                    .IsUnique(); // Prevents duplicate attendance records for same employee on same day

                entity.Property(e => e.Status)
                    .HasMaxLength(50);

                entity.Property(e => e.Shift)
                    .HasMaxLength(10);

                entity.Property(e => e.HoursWorked)
                    .HasPrecision(5, 2);

                entity.Property(e => e.OvertimeHours)
                    .HasPrecision(5, 2);
            });

            // EMEmaillogs Configuration
            modelBuilder.Entity<EMEmaillogs>(entity =>
            {
                entity.HasKey(e => e.id);

                entity.Property(e => e.Emaillogsid)
                    .HasMaxLength(100);

                entity.Property(e => e.Email)
                    .HasMaxLength(255);

                entity.HasOne(e => e.OtpUserRequest)
                    .WithMany()
                    .HasForeignKey("OtpUserRequestId")
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // EMOtpUserRequest Configuration
            modelBuilder.Entity<EMOtpUserRequest>(entity =>
            {
                entity.HasKey(e => e.id);

                entity.Property(e => e.OTPid)
                    .HasMaxLength(100);

                entity.Property(e => e.OTP)
                    .HasMaxLength(10);

                entity.HasIndex(e => e.OTPid);
            });

            // Seed initial data for roles
            modelBuilder.Entity<EMRoleBasedAccessControl>().HasData(
                new EMRoleBasedAccessControl
                {
                    Id = 1,
                    RoleName = "Admin",
                    Description = "Full system access",
                    CanViewEmployees = true,
                    CanEditEmployees = true,
                    CanDeleteEmployees = true,
                    CanViewAttendance = true,
                    CanEditAttendance = true,
                    CanGenerateReports = true,
                    CanManageUsers = true,
                    CanManageRoles = true,
                    CanAccessAdminPanel = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new EMRoleBasedAccessControl
                {
                    Id = 2,
                    RoleName = "Manager",
                    Description = "Manager level access",
                    CanViewEmployees = true,
                    CanEditEmployees = true,
                    CanDeleteEmployees = false,
                    CanViewAttendance = true,
                    CanEditAttendance = true,
                    CanGenerateReports = true,
                    CanManageUsers = false,
                    CanManageRoles = false,
                    CanAccessAdminPanel = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }, 
                new EMRoleBasedAccessControl
                {
                    Id = 3,
                    RoleName = "User",
                    Description = "Basic user access",
                    CanViewEmployees = false,
                    CanEditEmployees = false,
                    CanDeleteEmployees = false,
                    CanViewAttendance = true,
                    CanEditAttendance = false,
                    CanGenerateReports = false,
                    CanManageUsers = false,
                    CanManageRoles = false,
                    CanAccessAdminPanel = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            );
        }
    }
}