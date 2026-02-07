using oamswlatifose.Server.Model.security;

namespace oamswlatifose.Server.Repository.UserManagement.Interfaces
{
    /// <summary>
    /// Interface for user account data modification operations defining contracts for all create,
    /// update, delete, and authentication operations on user entities. This repository interface
    /// establishes the pattern for user identity management with comprehensive security controls.
    /// 
    /// <para>Command Operations Overview:</para>
    /// <para>- Secure user account creation with password hashing and salting</para>
    /// <para>- Authentication credential validation with brute force protection</para>
    /// <para>- Password change and reset workflows with token-based verification</para>
    /// <para>- Account state management (activation, deactivation, lockout)</para>
    /// <para>- Email verification and account status modifications</para>
    /// <para>- Role assignment updates for permission management</para>
    /// 
    /// <para>All methods implement defense-in-depth security principles,
    /// never expose sensitive credential information, and maintain comprehensive
    /// audit logs of all authentication and account modification events.</para>
    /// </summary>
    public interface IUserAccountCommandRepository
    {
        /// <summary>
        /// Creates a new user account with secure password hashing and comprehensive validation.
        /// Generates cryptographically secure salt and stores only password hash, never plaintext.
        /// </summary>
        /// <param name="user">The user account entity containing username, email, and role information</param>
        /// <param name="password">The plaintext password to be securely hashed and stored</param>
        /// <returns>A task representing the asynchronous operation with the newly created user account</returns>
        Task<EMAuthorizeruser> CreateUserAsync(EMAuthorizeruser user, string password);

        /// <summary>
        /// Updates an existing user account with modified profile information.
        /// Preserves security credentials and audit trail while allowing updates to non-sensitive fields.
        /// </summary>
        /// <param name="user">The user account entity containing updated property values</param>
        /// <returns>A task representing the asynchronous operation with the fully updated user entity</returns>
        Task<EMAuthorizeruser> UpdateUserAsync(EMAuthorizeruser user);

        /// <summary>
        /// Permanently removes a user account from the system.
        /// This operation is irreversible and requires appropriate authorization.
        /// </summary>
        /// <param name="id">The unique system identifier of the user account to delete</param>
        /// <returns>A task representing the asynchronous operation with boolean success indicator</returns>
        Task<bool> DeleteUserAsync(int id);

        /// <summary>
        /// Authenticates a user by verifying provided credentials against stored password hash.
        /// Implements secure verification with constant-time comparison and tracks failed attempts.
        /// </summary>
        /// <param name="username">The username of the account attempting authentication</param>
        /// <param name="password">The plaintext password to verify against stored hash</param>
        /// <returns>A task containing the authenticated user entity if credentials are valid; otherwise, null</returns>
        Task<EMAuthorizeruser> ValidateUserCredentialsAsync(string username, string password);

        /// <summary>
        /// Changes a user's password after verifying the current password for security.
        /// Generates new cryptographic salt and hash for the new password.
        /// </summary>
        /// <param name="userId">The unique identifier of the user changing their password</param>
        /// <param name="currentPassword">The user's current password for verification</param>
        /// <param name="newPassword">The new password to set for the account</param>
        /// <returns>A task representing the asynchronous operation with boolean success indicator</returns>
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);

        /// <summary>
        /// Resets a user's password using a secure token for identity verification.
        /// Typically used in "forgot password" workflows where current password is unknown.
        /// </summary>
        /// <param name="email">The email address associated with the user account</param>
        /// <param name="token">The password reset token for verification</param>
        /// <param name="newPassword">The new password to set for the account</param>
        /// <returns>A task representing the asynchronous operation with boolean success indicator</returns>
        Task<bool> ResetPasswordAsync(string email, string token, string newPassword);

        /// <summary>
        /// Generates a secure password reset token for a user account.
        /// Creates a cryptographically random token with configurable expiration period.
        /// </summary>
        /// <param name="email">The email address of the user requesting password reset</param>
        /// <returns>A task containing the generated reset token if user exists; otherwise, null</returns>
        Task<string> GeneratePasswordResetTokenAsync(string email);

        /// <summary>
        /// Updates the email verification status of a user account.
        /// Marks the user's email as verified and records the verification timestamp.
        /// </summary>
        /// <param name="userId">The unique identifier of the user to verify</param>
        /// <returns>A task representing the asynchronous operation with boolean success indicator</returns>
        Task<bool> VerifyEmailAsync(int userId);

        /// <summary>
        /// Toggles the active status of a user account for enabling or disabling system access.
        /// Deactivated users cannot authenticate or access system resources.
        /// </summary>
        /// <param name="userId">The unique identifier of the user account</param>
        /// <param name="isActive">The desired active state (true for active, false for inactive)</param>
        /// <returns>A task representing the asynchronous operation with the updated user entity</returns>
        Task<EMAuthorizeruser> SetUserActiveStatusAsync(int userId, bool isActive);
    }
}
