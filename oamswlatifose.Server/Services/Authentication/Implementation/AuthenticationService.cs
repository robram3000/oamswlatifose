using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using oamswlatifose.Server.DTO.User;
using oamswlatifose.Server.Middleware;
using oamswlatifose.Server.Repository.AuditManagement.Interfaces;
using oamswlatifose.Server.Repository.RoleManagement.Interfaces;
using oamswlatifose.Server.Repository.SessionManagement.Interfaces;
using oamswlatifose.Server.Repository.TokenManagement.Interfaces;
using oamswlatifose.Server.Repository.UserManagement.Interfaces;
using oamswlatifose.Server.Utilities.Security;

namespace oamswlatifose.Server.Services.Authentication.Implementation
{
    /// <summary>
    /// Comprehensive authentication service implementing secure login, registration,
    /// token management, and password workflows with complete audit logging.
    /// </summary>
    public class AuthenticationService : BaseService, IAuthenticationService
    {
        private readonly IUserAccountQueryRepository _userQueryRepository;
        private readonly IUserAccountCommandRepository _userCommandRepository;
        private readonly IJwtTokenManagementCommandRepository _tokenCommandRepository;
        private readonly IJwtTokenManagementQueryRepository _tokenQueryRepository;
        private readonly ISessionManagementCommandRepository _sessionCommandRepository;
        private readonly IAuthenticationAuditCommandRepository _auditCommandRepository;
        private readonly IRoleBasedAccessQueryRepository _roleQueryRepository;
        private readonly JwtTokenGenerator _tokenGenerator;
        private readonly IMapper _mapper;
        private readonly IValidator<LoginRequestDTO> _loginValidator;
        private readonly IValidator<RegisterRequestDTO> _registerValidator;
        private readonly IValidator<ChangePasswordDTO> _changePasswordValidator;
        private readonly IValidator<ForgotPasswordDTO> _forgotPasswordValidator;
        private readonly IValidator<ResetPasswordDTO> _resetPasswordValidator;

        public AuthenticationService(
            IUserAccountQueryRepository userQueryRepository,
            IUserAccountCommandRepository userCommandRepository,
            IJwtTokenManagementCommandRepository tokenCommandRepository,
            IJwtTokenManagementQueryRepository tokenQueryRepository,
            ISessionManagementCommandRepository sessionCommandRepository,
            IAuthenticationAuditCommandRepository auditCommandRepository,
            IRoleBasedAccessQueryRepository roleQueryRepository,
            JwtTokenGenerator tokenGenerator,
            IMapper mapper,
            IValidator<LoginRequestDTO> loginValidator,
            IValidator<RegisterRequestDTO> registerValidator,
            IValidator<ChangePasswordDTO> changePasswordValidator,
            IValidator<ForgotPasswordDTO> forgotPasswordValidator,
            IValidator<ResetPasswordDTO> resetPasswordValidator,
            ILogger<AuthenticationService> logger,
            IHttpContextAccessor httpContextAccessor,
            ICorrelationIdGenerator correlationIdGenerator)
            : base(logger, httpContextAccessor, correlationIdGenerator)
        {
            _userQueryRepository = userQueryRepository;
            _userCommandRepository = userCommandRepository;
            _tokenCommandRepository = tokenCommandRepository;
            _tokenQueryRepository = tokenQueryRepository;
            _sessionCommandRepository = sessionCommandRepository;
            _auditCommandRepository = auditCommandRepository;
            _roleQueryRepository = roleQueryRepository;
            _tokenGenerator = tokenGenerator;
            _mapper = mapper;
            _loginValidator = loginValidator;
            _registerValidator = registerValidator;
            _changePasswordValidator = changePasswordValidator;
            _forgotPasswordValidator = forgotPasswordValidator;
            _resetPasswordValidator = resetPasswordValidator;
        }

        public async Task<ServiceResponse<LoginResponseDTO>> LoginAsync(
            LoginRequestDTO loginRequest,
            string ipAddress,
            string userAgent)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    // Validate request
                    var validationResult = await _loginValidator.ValidateAsync(loginRequest);
                    if (!validationResult.IsValid)
                    {
                        await _auditCommandRepository.LogFailedAuthenticationAsync(
                            loginRequest.Username,
                            "Login",
                            "Validation failed",
                            ipAddress,
                            userAgent);

                        return ServiceResponse<LoginResponseDTO>.Failure(
                            "Invalid login credentials",
                            validationResult.Errors.Select(e => e.ErrorMessage));
                    }

                    // Authenticate user
                    var user = await _userCommandRepository.ValidateUserCredentialsAsync(
                        loginRequest.Username,
                        loginRequest.Password);

                    if (user == null)
                    {
                        await _auditCommandRepository.LogFailedAuthenticationAsync(
                            loginRequest.Username,
                            "Login",
                            "Invalid username or password",
                            ipAddress,
                            userAgent);

                        return ServiceResponse<LoginResponseDTO>.Failure("Invalid username or password");
                    }

                    if (!user.IsActive)
                    {
                        await _auditCommandRepository.LogFailedAuthenticationAsync(
                            loginRequest.Username,
                            "Login",
                            "Account is deactivated",
                            ipAddress,
                            userAgent);

                        return ServiceResponse<LoginResponseDTO>.Failure("Your account has been deactivated");
                    }

                    // Get user role for permissions
                    var role = await _roleQueryRepository.GetRoleByIdAsync(user.RoleId);
                    if (role == null || !role.IsActive)
                    {
                        await _auditCommandRepository.LogFailedAuthenticationAsync(
                            loginRequest.Username,
                            "Login",
                            "Invalid role assignment",
                            ipAddress,
                            userAgent);

                        return ServiceResponse<LoginResponseDTO>.Failure("Account configuration error");
                    }

                    // Generate tokens
                    var (accessToken, expiresAt) = _tokenGenerator.GenerateAccessToken(user, role);
                    var (refreshToken, refreshExpiresAt) = _tokenGenerator.GenerateRefreshToken();

                    // Create session
                    var session = new EMSession
                    {
                        UserId = user.Id,
                        SessionToken = PasswordHasher.GenerateSecureToken(32),
                        IPAddress = ipAddress,
                        UserAgent = userAgent,
                        DeviceType = DetermineDeviceType(userAgent),
                        LoginTime = DateTime.UtcNow,
                        ExpiresAt = expiresAt,
                        IsActive = true
                    };

                    await _sessionCommandRepository.CreateSessionAsync(session);

                    // Store tokens
                    var jwtToken = new EMJWT
                    {
                        UserId = user.Id,
                        Token = accessToken,
                        RefreshToken = refreshToken,
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = expiresAt,
                        RefreshTokenExpiresAt = refreshExpiresAt,
                        IsRevoked = false,
                        IPAddress = ipAddress,
                        UserAgent = userAgent
                    };

                    await _tokenCommandRepository.CreateTokenAsync(jwtToken);

                    // Log successful authentication
                    await _auditCommandRepository.LogSuccessfulAuthenticationAsync(
                        user.Id,
                        user.Username,
                        "Login",
                        ipAddress,
                        userAgent,
                        DetermineDeviceType(userAgent),
                        null,
                        $"Session ID: {session.Id}");

                    // Prepare response
                    var userDto = _mapper.Map<UserResponseDTO>(user);
                    userDto.RolePermissions = _mapper.Map<Dictionary<string, bool>>(role);

                    var response = new LoginResponseDTO
                    {
                        AccessToken = accessToken,
                        RefreshToken = refreshToken,
                        ExpiresAt = expiresAt,
                        User = userDto
                    };

                    _logger.LogInformation("User {Username} logged in successfully from {IpAddress}",
                        user.Username, ipAddress);

                    return ServiceResponse<LoginResponseDTO>.Success(response, "Login successful");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during login for user {Username}", loginRequest.Username);

                    await _auditCommandRepository.LogFailedAuthenticationAsync(
                        loginRequest.Username,
                        "Login",
                        $"System error: {ex.Message}",
                        ipAddress,
                        userAgent);

                    return ServiceResponse<LoginResponseDTO>.FromException(ex, "Login failed due to system error");
                }
            }, "LoginAsync");
        }

        public async Task<ServiceResponse<UserResponseDTO>> RegisterAsync(
            RegisterRequestDTO registerRequest,
            string ipAddress,
            string userAgent)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    // Validate request
                    var validationResult = await _registerValidator.ValidateAsync(registerRequest);
                    if (!validationResult.IsValid)
                    {
                        return ServiceResponse<UserResponseDTO>.Failure(
                            "Validation failed",
                            validationResult.Errors.Select(e => e.ErrorMessage));
                    }

                    // Check if user already exists
                    var existingUser = await _userQueryRepository.GetUserByEmailAsync(registerRequest.Email);
                    if (existingUser != null)
                    {
                        await _auditCommandRepository.LogFailedAuthenticationAsync(
                            registerRequest.Email,
                            "Registration",
                            "Email already registered",
                            ipAddress,
                            userAgent);

                        return ServiceResponse<UserResponseDTO>.Failure("Email already registered");
                    }

                    // Create user
                    var user = _mapper.Map<EMAuthorizeruser>(registerRequest);
                    var createdUser = await _userCommandRepository.CreateUserAsync(user, registerRequest.Password);

                    // Generate email verification token
                    var verificationToken = PasswordHasher.GenerateSecureToken();
                    // TODO: Send verification email

                    // Log registration
                    await _auditCommandRepository.LogSuccessfulAuthenticationAsync(
                        createdUser.Id,
                        createdUser.Username,
                        "Registration",
                        ipAddress,
                        userAgent,
                        DetermineDeviceType(userAgent),
                        null,
                        "Account created successfully");

                    var userDto = _mapper.Map<UserResponseDTO>(createdUser);

                    _logger.LogInformation("User {Username} registered successfully", createdUser.Username);

                    return ServiceResponse<UserResponseDTO>.Success(
                        userDto,
                        "Registration successful. Please verify your email.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during registration for email {Email}", registerRequest.Email);
                    return ServiceResponse<UserResponseDTO>.FromException(ex, "Registration failed");
                }
            }, "RegisterAsync");
        }

        public async Task<ServiceResponse<RefreshTokenResponseDTO>> RefreshTokenAsync(
            string refreshToken,
            string ipAddress,
            string userAgent)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    // Validate refresh token
                    var token = await _tokenQueryRepository.GetTokenByRefreshTokenAsync(refreshToken);

                    if (token == null || token.IsRevoked || token.RefreshTokenExpiresAt < DateTime.UtcNow)
                    {
                        await _auditCommandRepository.LogFailedAuthenticationAsync(
                            "Unknown",
                            "TokenRefresh",
                            "Invalid or expired refresh token",
                            ipAddress,
                            userAgent);

                        return ServiceResponse<RefreshTokenResponseDTO>.Failure("Invalid refresh token");
                    }

                    // Get user and role
                    var user = await _userQueryRepository.GetUserByIdAsync(token.UserId);
                    if (user == null || !user.IsActive)
                    {
                        await _auditCommandRepository.LogFailedAuthenticationAsync(
                            "Unknown",
                            "TokenRefresh",
                            "User not found or inactive",
                            ipAddress,
                            userAgent);

                        return ServiceResponse<RefreshTokenResponseDTO>.Failure("User account not available");
                    }

                    var role = await _roleQueryRepository.GetRoleByIdAsync(user.RoleId);
                    if (role == null || !role.IsActive)
                    {
                        return ServiceResponse<RefreshTokenResponseDTO>.Failure("Invalid role configuration");
                    }

                    // Generate new tokens
                    var (newAccessToken, expiresAt) = _tokenGenerator.GenerateAccessToken(user, role);
                    var (newRefreshToken, refreshExpiresAt) = _tokenGenerator.GenerateRefreshToken();

                    // Revoke old token and create new one
                    await _tokenCommandRepository.RevokeTokenAsync(token.Id, "Refreshed");

                    var newToken = new EMJWT
                    {
                        UserId = user.Id,
                        Token = newAccessToken,
                        RefreshToken = newRefreshToken,
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = expiresAt,
                        RefreshTokenExpiresAt = refreshExpiresAt,
                        IsRevoked = false,
                        IPAddress = ipAddress,
                        UserAgent = userAgent
                    };

                    await _tokenCommandRepository.CreateTokenAsync(newToken);

                    // Update session
                    var activeSessions = await _sessionCommandRepository.GetActiveSessionsByUserIdAsync(user.Id);
                    var session = activeSessions.FirstOrDefault();
                    if (session != null)
                    {
                        await _sessionCommandRepository.ExtendSessionExpirationAsync(session.Id, TimeSpan.FromHours(8));
                    }

                    var response = new RefreshTokenResponseDTO
                    {
                        AccessToken = newAccessToken,
                        RefreshToken = newRefreshToken,
                        ExpiresAt = expiresAt
                    };

                    _logger.LogInformation("Token refreshed for user {Username}", user.Username);

                    return ServiceResponse<RefreshTokenResponseDTO>.Success(response, "Token refreshed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during token refresh");
                    return ServiceResponse<RefreshTokenResponseDTO>.FromException(ex, "Token refresh failed");
                }
            }, "RefreshTokenAsync");
        }

        public async Task<ServiceResponse<bool>> LogoutAsync(int userId, string accessToken)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    // Revoke token
                    var token = await _tokenQueryRepository.GetTokenByAccessTokenAsync(accessToken);
                    if (token != null)
                    {
                        await _tokenCommandRepository.RevokeTokenAsync(token.Id, "User logout");
                    }

                    // Terminate active sessions
                    await _sessionCommandRepository.TerminateAllUserSessionsAsync(userId);

                    var user = await _userQueryRepository.GetUserByIdAsync(userId);

                    _logger.LogInformation("User {Username} logged out successfully", user?.Username ?? userId.ToString());

                    return ServiceResponse<bool>.Success(true, "Logout successful");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during logout for user {UserId}", userId);
                    return ServiceResponse<bool>.FromException(ex, "Logout failed");
                }
            }, "LogoutAsync");
        }

        public async Task<ServiceResponse<bool>> ChangePasswordAsync(
            int userId,
            ChangePasswordDTO changePasswordDto,
            string ipAddress)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    // Validate request
                    var validationResult = await _changePasswordValidator.ValidateAsync(changePasswordDto);
                    if (!validationResult.IsValid)
                    {
                        return ServiceResponse<bool>.Failure(
                            "Validation failed",
                            validationResult.Errors.Select(e => e.ErrorMessage));
                    }

                    // Change password
                    var result = await _userCommandRepository.ChangePasswordAsync(
                        userId,
                        changePasswordDto.CurrentPassword,
                        changePasswordDto.NewPassword);

                    if (!result)
                    {
                        await _auditCommandRepository.LogFailedAuthenticationAsync(
                            userId.ToString(),
                            "PasswordChange",
                            "Invalid current password",
                            ipAddress,
                            _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString());

                        return ServiceResponse<bool>.Failure("Current password is incorrect");
                    }

                    // Revoke all user tokens (force re-login)
                    await _tokenCommandRepository.RevokeAllUserTokensAsync(userId, "Password changed");

                    // Terminate all sessions
                    await _sessionCommandRepository.TerminateAllUserSessionsAsync(userId);

                    var user = await _userQueryRepository.GetUserByIdAsync(userId);

                    await _auditCommandRepository.LogPasswordChangeAsync(
                        userId,
                        user?.Username ?? userId.ToString(),
                        true,
                        null,
                        ipAddress,
                        _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString(),
                        "Password changed successfully");

                    _logger.LogInformation("Password changed for user {UserId}", userId);

                    return ServiceResponse<bool>.Success(true, "Password changed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error changing password for user {UserId}", userId);
                    return ServiceResponse<bool>.FromException(ex, "Password change failed");
                }
            }, "ChangePasswordAsync");
        }

        public async Task<ServiceResponse<string>> ForgotPasswordAsync(
            ForgotPasswordDTO forgotPasswordDto,
            string ipAddress)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    // Validate request
                    var validationResult = await _forgotPasswordValidator.ValidateAsync(forgotPasswordDto);
                    if (!validationResult.IsValid)
                    {
                        return ServiceResponse<string>.Failure(
                            "Validation failed",
                            validationResult.Errors.Select(e => e.ErrorMessage));
                    }

                    // Generate reset token
                    var resetToken = await _userCommandRepository.GeneratePasswordResetTokenAsync(forgotPasswordDto.Email);

                    if (resetToken != null)
                    {
                        // TODO: Send password reset email
                        _logger.LogInformation("Password reset token generated for {Email}", forgotPasswordDto.Email);
                    }

                    // Always return success to prevent email enumeration
                    return ServiceResponse<string>.Success(
                        resetToken ?? "Token generated",
                        "If your email is registered, you will receive password reset instructions");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in forgot password for {Email}", forgotPasswordDto.Email);
                    return ServiceResponse<string>.FromException(ex, "Password reset request failed");
                }
            }, "ForgotPasswordAsync");
        }

        public async Task<ServiceResponse<bool>> ResetPasswordAsync(
            ResetPasswordDTO resetPasswordDto,
            string ipAddress)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    // Validate request
                    var validationResult = await _resetPasswordValidator.ValidateAsync(resetPasswordDto);
                    if (!validationResult.IsValid)
                    {
                        return ServiceResponse<bool>.Failure(
                            "Validation failed",
                            validationResult.Errors.Select(e => e.ErrorMessage));
                    }

                    // Reset password
                    var result = await _userCommandRepository.ResetPasswordAsync(
                        resetPasswordDto.Email,
                        resetPasswordDto.Token,
                        resetPasswordDto.NewPassword);

                    if (!result)
                    {
                        return ServiceResponse<bool>.Failure("Invalid or expired reset token");
                    }

                    var user = await _userQueryRepository.GetUserByEmailAsync(resetPasswordDto.Email);

                    if (user != null)
                    {
                        // Revoke all tokens
                        await _tokenCommandRepository.RevokeAllUserTokensAsync(user.Id, "Password reset");

                        // Terminate all sessions
                        await _sessionCommandRepository.TerminateAllUserSessionsAsync(user.Id);

                        await _auditCommandRepository.LogPasswordResetAsync(
                            user.Id,
                            user.Username,
                            true,
                            null,
                            ipAddress,
                            _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString(),
                            "Password reset successful");
                    }

                    _logger.LogInformation("Password reset successful for {Email}", resetPasswordDto.Email);

                    return ServiceResponse<bool>.Success(true, "Password reset successful");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error resetting password for {Email}", resetPasswordDto.Email);
                    return ServiceResponse<bool>.FromException(ex, "Password reset failed");
                }
            }, "ResetPasswordAsync");
        }

        public async Task<ServiceResponse<bool>> VerifyEmailAsync(int userId, string token)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    var user = await _userQueryRepository.GetUserByIdAsync(userId);
                    if (user == null)
                    {
                        return ServiceResponse<bool>.Failure("User not found");
                    }

                    // TODO: Implement email verification token validation
                    var result = await _userCommandRepository.VerifyEmailAsync(userId);

                    if (result)
                    {
                        _logger.LogInformation("Email verified for user {Username}", user.Username);
                        return ServiceResponse<bool>.Success(true, "Email verified successfully");
                    }

                    return ServiceResponse<bool>.Failure("Email verification failed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error verifying email for user {UserId}", userId);
                    return ServiceResponse<bool>.FromException(ex, "Email verification failed");
                }
            }, "VerifyEmailAsync");
        }

        public async Task<ServiceResponse<UserResponseDTO>> GetCurrentUserAsync(int userId)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    var user = await _userQueryRepository.GetUserWithDetailsAsync(userId);

                    if (user == null)
                    {
                        return ServiceResponse<UserResponseDTO>.Failure("User not found");
                    }

                    var userDto = _mapper.Map<UserResponseDTO>(user);

                    if (user.Role != null)
                    {
                        userDto.RolePermissions = _mapper.Map<Dictionary<string, bool>>(user.Role);
                    }

                    return ServiceResponse<UserResponseDTO>.Success(userDto, "User retrieved successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving current user {UserId}", userId);
                    return ServiceResponse<UserResponseDTO>.FromException(ex, "Failed to retrieve user");
                }
            }, "GetCurrentUserAsync");
        }

        private string DetermineDeviceType(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return "Unknown";

            userAgent = userAgent.ToLower();

            if (userAgent.Contains("mobile") || (userAgent.Contains("android") && !userAgent.Contains("tablet")))
                return "Mobile";

            if (userAgent.Contains("tablet") || userAgent.Contains("ipad"))
                return "Tablet";

            if (userAgent.Contains("windows") || userAgent.Contains("mac") ||
                userAgent.Contains("linux") || userAgent.Contains("x11"))
                return "Desktop";

            return "Other";
        }
    }
}
