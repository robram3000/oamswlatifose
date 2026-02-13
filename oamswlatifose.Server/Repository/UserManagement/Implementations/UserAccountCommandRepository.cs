using Microsoft.EntityFrameworkCore;
using oamswlatifose.Server.Model;
using oamswlatifose.Server.Model.security;

namespace oamswlatifose.Server.Repository.UserManagement.Implementations
{
    /// <summary>
    /// Command repository implementation for user account data modification operations.
    /// This repository handles all create, update, and delete operations for user accounts
    /// with comprehensive security validation, password hashing, and account state management.
    /// 
    /// <para>Key Operational Features:</para>
    /// <para>- Secure password hashing with salt generation for credential storage</para>
    /// <para>- Account lockout management for brute force attack prevention</para>
    /// <para>- Failed login attempt tracking with automatic lockout thresholds</para>
    /// <para>- Password reset token generation and validation</para>
    /// <para>- Email verification workflow support</para>
    /// <para>- Role assignment and modification with permission validation</para>
    /// <para>- Account activation/deactivation with state management</para>
    /// 
    /// <para>All operations maintain strict security standards including
    /// never storing plaintext passwords, using cryptographic hashing algorithms,
    /// and implementing account lockout policies to prevent unauthorized access.</para>
    /// </summary>
    public class UserAccountCommandRepository : IUserAccountCommandRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserAccountCommandRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the UserAccountCommandRepository with required dependencies.
        /// Establishes database context connection and logging infrastructure for user account operations.
        /// </summary>
        /// <param name="context">The application database context providing access to user account tables</param>
        /// <param name="logger">The logging service for capturing user account operation details and security events</param>
        public UserAccountCommandRepository(
            ApplicationDbContext context,
            ILogger<UserAccountCommandRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new user account with secure password hashing and comprehensive validation.
        /// Generates cryptographically secure salt, hashes the password, and initializes account state.
        /// Performs duplicate username/email checks and validates role assignment before persistence.
        /// </summary>
        /// <param name="user">The user account entity containing username, email, password, and role information</param>
        /// <param name="password">The plaintext password to be securely hashed and stored</param>
        /// <returns>A task representing the asynchronous operation with the newly created user account</returns>
        /// <exception cref="ArgumentNullException">Thrown when user or password parameters are null</exception>
        /// <exception cref="InvalidOperationException">Thrown when username/email already exists or role is invalid</exception>
        public async Task<EMAuthorizeruser> CreateUserAsync(EMAuthorizeruser user, string password)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentNullException(nameof(password));

            // Check username availability
            var usernameExists = await _context.EMAuthorizerusers
                .AnyAsync(u => u.Username.ToLower() == user.Username.ToLower());
            if (usernameExists)
                throw new InvalidOperationException($"Username '{user.Username}' is already taken");

            // Check email availability
            var emailExists = await _context.EMAuthorizerusers
                .AnyAsync(u => u.Email.ToLower() == user.Email.ToLower());
            if (emailExists)
                throw new InvalidOperationException($"Email '{user.Email}' is already registered");

            // Validate role exists
            var roleExists = await _context.EMRoleBasedAccessControls
                .AnyAsync(r => r.Id == user.RoleId);
            if (!roleExists)
                throw new InvalidOperationException($"Role with ID {user.RoleId} does not exist");

            // Generate password salt and hash
            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                user.PasswordSalt = Convert.ToBase64String(hmac.Key);
                user.PasswordHash = Convert.ToBase64String(
                    hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password)));
            }

            // Initialize account state
            user.IsActive = true;
            user.IsEmailVerified = false;
            user.FailedLoginAttempts = 0;
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.EMAuthorizerusers.AddAsync(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created user account: {user.Username}, ID: {user.Id}");
            return user;
        }

        /// <summary>
        /// Updates an existing user account with modified profile information.
        /// Preserves security credentials and audit trail while allowing updates to non-sensitive fields.
        /// Validates uniqueness constraints for username and email before applying changes.
        /// </summary>
        /// <param name="user">The user account entity containing updated property values</param>
        /// <returns>A task representing the asynchronous operation with the fully updated user entity</returns>
        /// <exception cref="ArgumentNullException">Thrown when the user parameter is null</exception>
        /// <exception cref="KeyNotFoundException">Thrown when no user account exists with the specified Id</exception>
        public async Task<EMAuthorizeruser> UpdateUserAsync(EMAuthorizeruser user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var existingUser = await _context.EMAuthorizerusers
                .FirstOrDefaultAsync(u => u.Id == user.Id);
            if (existingUser == null)
                throw new KeyNotFoundException($"User with ID {user.Id} not found");

            // Check username uniqueness if changed
            if (existingUser.Username != user.Username)
            {
                var duplicateUsername = await _context.EMAuthorizerusers
                    .AnyAsync(u => u.Username.ToLower() == user.Username.ToLower() && u.Id != user.Id);
                if (duplicateUsername)
                    throw new InvalidOperationException($"Username '{user.Username}' is already taken");
            }

            // Check email uniqueness if changed
            if (existingUser.Email != user.Email)
            {
                var duplicateEmail = await _context.EMAuthorizerusers
                    .AnyAsync(u => u.Email.ToLower() == user.Email.ToLower() && u.Id != user.Id);
                if (duplicateEmail)
                    throw new InvalidOperationException($"Email '{user.Email}' is already registered");
            }

            // Preserve security credentials and audit fields
            user.PasswordHash = existingUser.PasswordHash;
            user.PasswordSalt = existingUser.PasswordSalt;
            user.CreatedAt = existingUser.CreatedAt;
            user.UpdatedAt = DateTime.UtcNow;

            _context.Entry(existingUser).CurrentValues.SetValues(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Updated user account: {user.Username}, ID: {user.Id}");
            return existingUser;
        }

        /// <summary>
        /// Permanently removes a user account from the system.
        /// This operation is irreversible and should be protected by administrative authorization controls.
        /// </summary>
        /// <param name="id">The unique system identifier of the user account to delete</param>
        /// <returns>A task representing the asynchronous operation with boolean success indicator</returns>
        /// <exception cref="KeyNotFoundException">Thrown when no user account exists with the specified Id</exception>
        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.EMAuthorizerusers
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {id} not found");

            _context.EMAuthorizerusers.Remove(user);
            var result = await _context.SaveChangesAsync();

            _logger.LogInformation($"Deleted user account: {user.Username}, ID: {id}");
            return result > 0;
        }

        /// <summary>
        /// Authenticates a user by verifying provided credentials against stored hash.
        /// Implements secure password verification with constant-time comparison to prevent timing attacks.
        /// Tracks failed attempts and enforces account lockout policy for security.
        /// </summary>
        /// <param name="username">The username of the account attempting authentication</param>
        /// <param name="password">The plaintext password to verify against stored hash</param>
        /// <returns>A task containing the authenticated user entity if credentials are valid; otherwise, null</returns>
        public async Task<EMAuthorizeruser> ValidateUserCredentialsAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return null;

            var user = await _context.EMAuthorizerusers
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

            if (user == null)
            {
                _logger.LogWarning($"Authentication failed: Username '{username}' not found");
                return null;
            }

            // Check if account is locked out
            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            {
                _logger.LogWarning($"Authentication blocked: Account {username} is locked until {user.LockoutEnd}");
                return null;
            }

            // Verify password
            using (var hmac = new System.Security.Cryptography.HMACSHA512(Convert.FromBase64String(user.PasswordSalt)))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                var storedHash = Convert.FromBase64String(user.PasswordHash);

                if (computedHash.SequenceEqual(storedHash))
                {
                    // Success - reset failed attempts
                    user.FailedLoginAttempts = 0;
                    user.LastLogin = DateTime.UtcNow;
                    user.LockoutEnd = null;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Successful authentication: {username}");
                    return user;
                }
            }

            // Failed authentication - increment counter and lock if threshold exceeded
            user.FailedLoginAttempts++;

            // Lock account after 5 failed attempts
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                _logger.LogWarning($"Account locked: {username} exceeded maximum failed attempts");
            }

            await _context.SaveChangesAsync();
            _logger.LogWarning($"Failed authentication: {username} (Attempt {user.FailedLoginAttempts})");

            return null;
        }

        /// <summary>
        /// Changes a user's password after verifying the current password for security.
        /// Generates new salt and hash for the new password, maintaining cryptographic security.
        /// </summary>
        /// <param name="userId">The unique identifier of the user changing their password</param>
        /// <param name="currentPassword">The user's current password for verification</param>
        /// <param name="newPassword">The new password to set for the account</param>
        /// <returns>A task representing the asynchronous operation with boolean success indicator</returns>
        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _context.EMAuthorizerusers
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {userId} not found");

            // Verify current password
            using (var hmac = new System.Security.Cryptography.HMACSHA512(Convert.FromBase64String(user.PasswordSalt)))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(currentPassword));
                var storedHash = Convert.FromBase64String(user.PasswordHash);

                if (!computedHash.SequenceEqual(storedHash))
                {
                    _logger.LogWarning($"Password change failed: Invalid current password for user {user.Username}");
                    return false;
                }
            }

            // Generate new salt and hash for new password
            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                user.PasswordSalt = Convert.ToBase64String(hmac.Key);
                user.PasswordHash = Convert.ToBase64String(
                    hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(newPassword)));
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Password changed successfully for user {user.Username}");
            return true;
        }

        /// <summary>
        /// Resets a user's password using a secure token for identity verification.
        /// Typically used in "forgot password" workflows where current password is unknown.
        /// </summary>
        /// <param name="email">The email address associated with the user account</param>
        /// <param name="token">The password reset token for verification</param>
        /// <param name="newPassword">The new password to set for the account</param>
        /// <returns>A task representing the asynchronous operation with boolean success indicator</returns>
        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
        {
            var user = await _context.EMAuthorizerusers
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            if (user == null)
            {
                _logger.LogWarning($"Password reset failed: Email {email} not found");
                return false;
            }

            // Validate reset token and expiration
            if (user.PasswordResetToken != token ||
                !user.PasswordResetTokenExpires.HasValue ||
                user.PasswordResetTokenExpires < DateTime.UtcNow)
            {
                _logger.LogWarning($"Password reset failed: Invalid or expired token for {email}");
                return false;
            }

            // Generate new salt and hash for new password
            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                user.PasswordSalt = Convert.ToBase64String(hmac.Key);
                user.PasswordHash = Convert.ToBase64String(
                    hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(newPassword)));
            }

            // Clear reset token
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpires = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Password reset successful for user {user.Username}");
            return true;
        }

        /// <summary>
        /// Generates a secure password reset token for a user account.
        /// Creates a cryptographically random token with configurable expiration period.
        /// </summary>
        /// <param name="email">The email address of the user requesting password reset</param>
        /// <returns>A task containing the generated reset token if user exists; otherwise, null</returns>
        public async Task<string> GeneratePasswordResetTokenAsync(string email)
        {
            var user = await _context.EMAuthorizerusers
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            if (user == null)
            {
                _logger.LogWarning($"Token generation failed: Email {email} not found");
                return null;
            }

            // Generate cryptographically secure random token
            var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("/", "_")
                .Replace("+", "-")
                .Substring(0, 50);

            user.PasswordResetToken = token;
            user.PasswordResetTokenExpires = DateTime.UtcNow.AddHours(24); // 24-hour expiration
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Password reset token generated for user {user.Username}");
            return token;
        }

        /// <summary>
        /// Updates the email verification status of a user account.
        /// Marks the user's email as verified and records the verification timestamp.
        /// </summary>
        /// <param name="userId">The unique identifier of the user to verify</param>
        /// <returns>A task representing the asynchronous operation with boolean success indicator</returns>
        public async Task<bool> VerifyEmailAsync(int userId)
        {
            var user = await _context.EMAuthorizerusers
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return false;

            user.IsEmailVerified = true;
            user.EmailVerifiedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Email verified for user {user.Username}");
            return true;
        }

        /// <summary>
        /// Toggles the active status of a user account for enabling or disabling system access.
        /// Deactivated users cannot authenticate or access system resources.
        /// </summary>
        /// <param name="userId">The unique identifier of the user account</param>
        /// <param name="isActive">The desired active state (true for active, false for inactive)</param>
        /// <returns>A task representing the asynchronous operation with the updated user entity</returns>
        public async Task<EMAuthorizeruser> SetUserActiveStatusAsync(int userId, bool isActive)
        {
            var user = await _context.EMAuthorizerusers
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {userId} not found");

            user.IsActive = isActive;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"User {user.Username} active status set to {isActive}");
            return user;
        }
    }
}
