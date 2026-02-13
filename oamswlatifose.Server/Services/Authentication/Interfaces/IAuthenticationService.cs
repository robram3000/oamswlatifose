using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using oamswlatifose.Server.DTO.User;
using oamswlatifose.Server.Repository.AuditManagement.Interfaces;
using oamswlatifose.Server.Repository.RoleManagement.Interfaces;
using oamswlatifose.Server.Repository.SessionManagement.Interfaces;
using oamswlatifose.Server.Repository.TokenManagement.Interfaces;
using oamswlatifose.Server.Repository.UserManagement.Interfaces;
using oamswlatifose.Server.Utilities.Security;

namespace oamswlatifose.Server.Services.Authentication.Interfaces
{
    /// <summary>
    /// Service interface for authentication and authorization operations.
    /// Handles user login, registration, token management, and security workflows.
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Authenticates a user and generates access tokens.
        /// </summary>
        Task<ServiceResponse<LoginResponseDTO>> LoginAsync(LoginRequestDTO loginRequest, string ipAddress, string userAgent);

        /// <summary>
        /// Registers a new user account.
        /// </summary>
        Task<ServiceResponse<UserResponseDTO>> RegisterAsync(RegisterRequestDTO registerRequest, string ipAddress, string userAgent);

        /// <summary>
        /// Refreshes an expired access token using a refresh token.
        /// </summary>
        Task<ServiceResponse<RefreshTokenResponseDTO>> RefreshTokenAsync(string refreshToken, string ipAddress, string userAgent);

        /// <summary>
        /// Logs out a user by revoking their tokens and terminating sessions.
        /// </summary>
        Task<ServiceResponse<bool>> LogoutAsync(int userId, string accessToken);

        /// <summary>
        /// Changes a user's password.
        /// </summary>
        Task<ServiceResponse<bool>> ChangePasswordAsync(int userId, ChangePasswordDTO changePasswordDto, string ipAddress);

        /// <summary>
        /// Initiates password reset process.
        /// </summary>
        Task<ServiceResponse<string>> ForgotPasswordAsync(ForgotPasswordDTO forgotPasswordDto, string ipAddress);

        /// <summary>
        /// Resets password using reset token.
        /// </summary>
        Task<ServiceResponse<bool>> ResetPasswordAsync(ResetPasswordDTO resetPasswordDto, string ipAddress);

        /// <summary>
        /// Verifies user email address.
        /// </summary>
        Task<ServiceResponse<bool>> VerifyEmailAsync(int userId, string token);

        /// <summary>
        /// Gets the current authenticated user.
        /// </summary>
        Task<ServiceResponse<UserResponseDTO>> GetCurrentUserAsync(int userId);
    }
}
