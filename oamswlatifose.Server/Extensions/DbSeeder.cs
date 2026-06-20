using Microsoft.EntityFrameworkCore;
using oamswlatifose.Server.Model;
using oamswlatifose.Server.Model.branches;
using oamswlatifose.Server.Model.occurance;
using oamswlatifose.Server.Model.security;
using oamswlatifose.Server.Model.user;
using oamswlatifose.Server.Utilities.Security;

namespace oamswlatifose.Server.Extensions
{
    /// <summary>
    /// Development seeding: applies pending migrations and creates one login-able account per role
    /// (Admin / HR / User) — each linked to an employee with a default schedule — plus an example
    /// branch, so the attendance/OTP flow can be tested immediately. Idempotent.
    /// </summary>
    public static class DbSeeder
    {
        public const string DemoPassword = "Demo@123";

        // One account per role. RoleId matches the seeded roles (1=Admin, 2=HR, 3=User).
        // "User" is the basic employee who clocks in. EmployeeIDs are 200x to avoid colliding
        // with any earlier "demo" seed.
        private sealed record SeedAccount(
            string Username, int RoleId, int EmployeeId,
            string First, string Last, string Position, string Department, string Tag);

        private static readonly SeedAccount[] Accounts =
        {
            new("admin", 1, 2001, "System", "Admin",    "Administrator", "Administration",  "admin"),
            new("hr",    2, 2002, "HR",     "Officer",   "HR Officer",    "Human Resources", "hr"),
            new("user",  3, 2003, "Demo",   "Employee",  "Staff",         "Operations",      "user"),
        };

        public static async Task SeedDevAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var logger = sp.GetRequiredService<ILogger<ApplicationDbContext>>();
            var db = sp.GetRequiredService<ApplicationDbContext>();
            var config = sp.GetRequiredService<IConfiguration>();

            try
            {
                // OTP emails go to the employee address — default to the SMTP inbox so they're
                // deliverable. Per-role accounts use +tag aliases (same inbox, distinct addresses).
                var seedEmail = config["DevSeed:Email"]
                    ?? config["SmtpSettings:UserName"]
                    ?? "demo@example.com";

                var created = new List<string>();

                foreach (var a in Accounts)
                {
                    // Reset lockout for existing seed accounts so a bad-attempt streak never
                    // permanently locks out the demo logins across deployments.
                    var existing = await db.EMAuthorizerusers
                        .FirstOrDefaultAsync(u => u.Username == a.Username);
                    if (existing != null)
                    {
                        if (existing.FailedLoginAttempts > 0 || existing.LockoutEnd.HasValue)
                        {
                            existing.FailedLoginAttempts = 0;
                            existing.LockoutEnd = null;
                            await db.SaveChangesAsync();
                            logger.LogInformation("Seed account '{User}' lockout cleared.", a.Username);
                        }
                        continue;
                    }

                    var email = Alias(seedEmail, a.Tag);

                    // Employee (attendance/OTP resolve from the token's employee_id claim).
                    var employee = await db.EMEmployees.FirstOrDefaultAsync(e => e.EmployeeID == a.EmployeeId);
                    if (employee == null)
                    {
                        employee = new EMEmployees
                        {
                            EmployeeID = a.EmployeeId,
                            FirstName = a.First,
                            LastName = a.Last,
                            Email = email,
                            Phone = "0000000000",
                            Position = a.Position,
                            Department = a.Department,
                            City = "HQ",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                        };
                        db.EMEmployees.Add(employee);
                        await db.SaveChangesAsync();
                    }

                    var (hash, salt) = PasswordHasher.HashPassword(DemoPassword);
                    db.EMAuthorizerusers.Add(new EMAuthorizeruser
                    {
                        Username = a.Username,
                        Email = email,
                        PasswordHash = hash,
                        PasswordSalt = salt,
                        RoleId = a.RoleId,
                        EmployeeId = employee.Id,
                        IsActive = true,
                        IsEmailVerified = true,
                        EmailVerifiedAt = DateTime.UtcNow,
                        PasswordResetToken = "",   // column is NOT NULL
                        CreatedAt = DateTime.UtcNow,
                    });

                    // Default schedule (08:00–17:00, 5-min grace) so clock-ins are graded.
                    if (!await db.EMWorkSchedules.AnyAsync(s => s.EmployeeId == employee.Id))
                    {
                        db.EMWorkSchedules.Add(new EMWorkSchedule
                        {
                            EmployeeId = employee.Id,
                            StartTime = new TimeSpan(8, 0, 0),
                            EndTime = new TimeSpan(17, 0, 0),
                            GraceMinutes = 5,
                            WorkDays = "Mon,Tue,Wed,Thu,Fri",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                        });
                    }

                    await db.SaveChangesAsync();
                    created.Add(a.Username);
                }

                // Example office branch (edit it, or use "Use my current location" in the UI).
                if (!await db.EMBranches.AnyAsync())
                {
                    db.EMBranches.Add(new EMBranch
                    {
                        Name = "Head Office (example)",
                        Address = "Edit me to your office coordinates",
                        Latitude = 14.599512,
                        Longitude = 120.984222,
                        RadiusMeters = 150,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                    });
                    await db.SaveChangesAsync();
                }

                logger.LogInformation(
                    "Dev seed ready → accounts admin / hr / user (password '{Pass}'). New this run: [{Created}]. OTP emails go to {Email} (+tag aliases).",
                    DemoPassword, string.Join(", ", created), seedEmail);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Dev seeding failed. Ensure SQL Server is reachable and run 'dotnet ef database update'.");
            }
        }

        // "name@host" + tag → "name+tag@host" (Gmail-style alias: same inbox, unique address).
        private static string Alias(string email, string tag)
        {
            var at = email.IndexOf('@');
            return at > 0 ? $"{email[..at]}+{tag}{email[at..]}" : email;
        }
    }
}
