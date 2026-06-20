using Microsoft.EntityFrameworkCore;
using oamswlatifose.Server.DTO.User;
using oamswlatifose.Server.Model;
using oamswlatifose.Server.Model.security;
using oamswlatifose.Server.Model.user;
using oamswlatifose.Server.Services.UserProvisioning.Interfaces;
using oamswlatifose.Server.Utilities.Security;

namespace oamswlatifose.Server.Services.UserProvisioning.Implementation
{
    /// <summary>EF-backed account provisioning for Admin/HR.</summary>
    public class UserProvisioningService : IUserProvisioningService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<UserProvisioningService> _logger;

        public UserProvisioningService(ApplicationDbContext db, ILogger<UserProvisioningService> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ServiceResponse<List<UserAccountSummaryDTO>>> ListAsync()
        {
            try
            {
                var users = await _db.EMAuthorizerusers
                    .Include(u => u.Role)
                    .Include(u => u.Employee)
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync();

                var list = users.Select(u => new UserAccountSummaryDTO
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    RoleId = u.RoleId,
                    RoleName = u.Role?.RoleName,
                    EmployeeId = u.EmployeeId,
                    EmployeeName = u.Employee != null ? $"{u.Employee.FirstName} {u.Employee.LastName}" : null,
                    Department = u.Employee?.Department,
                    IsActive = u.IsActive,
                    CreatedAtFormatted = u.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                }).ToList();

                return ServiceResponse<List<UserAccountSummaryDTO>>.SuccessResult(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing user accounts");
                return ServiceResponse<List<UserAccountSummaryDTO>>.FromException(ex, "Failed to list users");
            }
        }

        public async Task<ServiceResponse<UserAccountSummaryDTO>> CreateAsync(CreateUserAccountDTO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
                    return ServiceResponse<UserAccountSummaryDTO>.FailureResult("Username and password are required");

                var username = dto.Username.Trim();
                var email = dto.Email.Trim();

                if (await _db.EMAuthorizerusers.AnyAsync(u => u.Username.ToLower() == username.ToLower()))
                    return ServiceResponse<UserAccountSummaryDTO>.FailureResult("That username is already taken");

                if (await _db.EMAuthorizerusers.AnyAsync(u => u.Email.ToLower() == email.ToLower()))
                    return ServiceResponse<UserAccountSummaryDTO>.FailureResult("An account with that email already exists");

                var role = await _db.EMRoleBasedAccessControls.FirstOrDefaultAsync(r => r.Id == dto.RoleId && r.IsActive);
                if (role == null)
                    return ServiceResponse<UserAccountSummaryDTO>.FailureResult("Selected role does not exist");

                // Next employee badge number (EmployeeID is required + unique).
                var maxBadge = await _db.EMEmployees.AnyAsync()
                    ? await _db.EMEmployees.MaxAsync(e => e.EmployeeID)
                    : 1000;

                var employee = new EMEmployees
                {
                    EmployeeID = maxBadge + 1,
                    FirstName = dto.FirstName.Trim(),
                    LastName = dto.LastName.Trim(),
                    Email = email,
                    Phone = dto.Phone?.Trim() ?? "",
                    Position = dto.Position?.Trim() ?? "",
                    Department = dto.Department?.Trim() ?? "",
                    City = "",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };
                _db.EMEmployees.Add(employee);
                await _db.SaveChangesAsync();

                var (hash, salt) = PasswordHasher.HashPassword(dto.Password);
                var account = new EMAuthorizeruser
                {
                    Username = username,
                    Email = email,
                    PasswordHash = hash,
                    PasswordSalt = salt,
                    RoleId = role.Id,
                    EmployeeId = employee.Id,
                    IsActive = true,
                    IsEmailVerified = true,
                    EmailVerifiedAt = DateTime.UtcNow,
                    PasswordResetToken = "",
                    CreatedAt = DateTime.UtcNow,
                };
                _db.EMAuthorizerusers.Add(account);
                await _db.SaveChangesAsync();

                _logger.LogInformation("User account '{Username}' ({Role}) created for employee {EmployeeId}",
                    username, role.RoleName, employee.Id);

                return ServiceResponse<UserAccountSummaryDTO>.SuccessResult(new UserAccountSummaryDTO
                {
                    Id = account.Id,
                    Username = account.Username,
                    Email = account.Email,
                    RoleId = role.Id,
                    RoleName = role.RoleName,
                    EmployeeId = employee.Id,
                    EmployeeName = $"{employee.FirstName} {employee.LastName}",
                    Department = employee.Department,
                    IsActive = true,
                    CreatedAtFormatted = account.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                }, $"User '{username}' created");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user account");
                return ServiceResponse<UserAccountSummaryDTO>.FromException(ex, "Failed to create user");
            }
        }

        public async Task<ServiceResponse<List<RoleOptionDTO>>> GetRoleOptionsAsync()
        {
            try
            {
                var roles = await _db.EMRoleBasedAccessControls
                    .Where(r => r.IsActive)
                    .OrderBy(r => r.Id)
                    .Select(r => new RoleOptionDTO { Id = r.Id, Name = r.RoleName })
                    .ToListAsync();

                return ServiceResponse<List<RoleOptionDTO>>.SuccessResult(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing roles");
                return ServiceResponse<List<RoleOptionDTO>>.FromException(ex, "Failed to list roles");
            }
        }
    }
}
