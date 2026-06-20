using Microsoft.EntityFrameworkCore;
using oamswlatifose.Server.Model;
using oamswlatifose.Server.Model.security;
using oamswlatifose.Server.Repository.UserManagement.Interfaces;

namespace oamswlatifose.Server.Repository.UserManagement.Implementations
{
    /// <summary>
    /// Query repository implementation for user account data retrieval operations with comprehensive
    /// user lookup, authentication support, and role-based filtering. This repository provides
    /// read-only access to user account information essential for authentication, authorization,
    /// and user management workflows.
    /// 
    /// <para>Core Capabilities:</para>
    /// <para>- User lookup by various identifiers (ID, username, email)</para>
    /// <para>- Authentication credential verification support</para>
    /// <para>- Role-based user filtering and assignment analysis</para>
    /// <para>- Account status monitoring (active, locked, verified)</para>
    /// <para>- Paginated user listings for administrative interfaces</para>
    /// <para>- Employee-user relationship navigation</para>
    /// </summary>
    public class UserAccountQueryRepository : IUserAccountQueryRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserAccountQueryRepository> _logger;

        public UserAccountQueryRepository(
            ApplicationDbContext context,
            ILogger<UserAccountQueryRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<EMAuthorizeruser>> GetAllUsersAsync()
        {
            _logger.LogDebug("Retrieving all user accounts");
            return await _context.EMAuthorizerusers
                .Include(u => u.Role)
                .Include(u => u.Employee)
                .OrderBy(u => u.Username)
                .ToListAsync();
        }

        public async Task<IEnumerable<EMAuthorizeruser>> GetUsersPaginatedAsync(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            _logger.LogDebug("Retrieving users page {PageNumber} with page size {PageSize}", pageNumber, pageSize);

            return await _context.EMAuthorizerusers
                .Include(u => u.Role)
                .Include(u => u.Employee)
                .OrderBy(u => u.Username)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<EMAuthorizeruser> GetUserByIdAsync(int id)
        {
            _logger.LogDebug("Retrieving user with ID: {Id}", id);
            return await _context.EMAuthorizerusers
                .Include(u => u.Role)
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<EMAuthorizeruser> GetUserByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be null or empty", nameof(username));

            _logger.LogDebug("Retrieving user with username: {Username}", username);
            return await _context.EMAuthorizerusers
                .Include(u => u.Role)
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        }

        public async Task<EMAuthorizeruser> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be null or empty", nameof(email));

            _logger.LogDebug("Retrieving user with email: {Email}", email);
            return await _context.EMAuthorizerusers
                .Include(u => u.Role)
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<IEnumerable<EMAuthorizeruser>> GetUsersByRoleIdAsync(int roleId)
        {
            _logger.LogDebug("Retrieving users with role ID: {RoleId}", roleId);
            return await _context.EMAuthorizerusers
                .Include(u => u.Role)
                .Include(u => u.Employee)
                .Where(u => u.RoleId == roleId)
                .OrderBy(u => u.Username)
                .ToListAsync();
        }

        public async Task<EMAuthorizeruser> GetUserByEmployeeIdAsync(int employeeId)
        {
            _logger.LogDebug("Retrieving user with employee ID: {EmployeeId}", employeeId);
            return await _context.EMAuthorizerusers
                .Include(u => u.Role)
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
        }

        public async Task<bool> UserExistsAsync(int id)
        {
            return await _context.EMAuthorizerusers.AnyAsync(u => u.Id == id);
        }

        public async Task<bool> IsUsernameAvailableAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            return !await _context.EMAuthorizerusers.AnyAsync(u => u.Username.ToLower() == username.ToLower());
        }

        public async Task<bool> IsEmailAvailableAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return !await _context.EMAuthorizerusers.AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<IEnumerable<EMAuthorizeruser>> GetActiveUsersAsync()
        {
            _logger.LogDebug("Retrieving active users");
            return await _context.EMAuthorizerusers
                .Include(u => u.Role)
                .Include(u => u.Employee)
                .Where(u => u.IsActive)
                .OrderBy(u => u.Username)
                .ToListAsync();
        }

        public async Task<IEnumerable<EMAuthorizeruser>> GetLockedOutUsersAsync()
        {
            _logger.LogDebug("Retrieving locked out users");
            var now = DateTime.UtcNow;
            return await _context.EMAuthorizerusers
                .Include(u => u.Role)
                .Include(u => u.Employee)
                .Where(u => u.LockoutEnd.HasValue && u.LockoutEnd > now)
                .OrderBy(u => u.Username)
                .ToListAsync();
        }

        public async Task<int> GetTotalUserCountAsync()
        {
            return await _context.EMAuthorizerusers.CountAsync();
        }

        public async Task<EMAuthorizeruser> GetUserWithDetailsAsync(int id)
        {
            _logger.LogDebug("Retrieving user with details for ID: {Id}", id);
            return await _context.EMAuthorizerusers
                .Include(u => u.Role)
                .Include(u => u.Employee)
                .Include(u => u.Sessions.Where(s => s.IsActive))
                .Include(u => u.AuthLogs.OrderByDescending(l => l.Timestamp).Take(10))
                .FirstOrDefaultAsync(u => u.Id == id);
        }
    }
}
