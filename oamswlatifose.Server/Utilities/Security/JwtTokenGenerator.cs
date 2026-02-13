using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using oamswlatifose.Server.Model.security;
using System.Security.Claims;
using System.Text;

namespace oamswlatifose.Server.Utilities.Security
{
    /// <summary>
    /// Configuration settings for JWT token generation.
    /// </summary>
    public class JwtSettings
    {
        public string Secret { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public int AccessTokenExpirationMinutes { get; set; }
        public int RefreshTokenExpirationDays { get; set; }
    }

    /// <summary>
    /// Provides JWT token generation, validation, and refresh token management services.
    /// Implements secure token creation with appropriate claims, expiration policies,
    /// and cryptographic signing.
    /// </summary>
    public class JwtTokenGenerator
    {
        private readonly JwtSettings _jwtSettings;
        private readonly TokenValidationParameters _tokenValidationParameters;

        public JwtTokenGenerator(IOptions<JwtSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;
            _tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtSettings.Secret)),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        }

        /// <summary>
        /// Generates a JWT access token for an authenticated user with role claims.
        /// </summary>
        /// <param name="user">The authenticated user</param>
        /// <param name="role">The user's role with permissions</param>
        /// <returns>JWT token string and expiration datetime</returns>
        public (string token, DateTime expiresAt) GenerateAccessToken(EMAuthorizeruser user, EMRoleBasedAccessControl role)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("user_id", user.Id.ToString()),
                new Claim("role_id", user.RoleId.ToString()),
                new Claim("role_name", role.RoleName)
            };

            // Add permission claims
            if (role.CanViewEmployees) claims.Add(new Claim("permission", "view_employees"));
            if (role.CanEditEmployees) claims.Add(new Claim("permission", "edit_employees"));
            if (role.CanDeleteEmployees) claims.Add(new Claim("permission", "delete_employees"));
            if (role.CanViewAttendance) claims.Add(new Claim("permission", "view_attendance"));
            if (role.CanEditAttendance) claims.Add(new Claim("permission", "edit_attendance"));
            if (role.CanGenerateReports) claims.Add(new Claim("permission", "generate_reports"));
            if (role.CanManageUsers) claims.Add(new Claim("permission", "manage_users"));
            if (role.CanManageRoles) claims.Add(new Claim("permission", "manage_roles"));
            if (role.CanAccessAdminPanel) claims.Add(new Claim("permission", "admin_access"));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiresAt,
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return (tokenHandler.WriteToken(token), expiresAt);
        }

        /// <summary>
        /// Generates a cryptographically secure refresh token.
        /// </summary>
        /// <returns>Refresh token string and expiration datetime</returns>
        public (string token, DateTime expiresAt) GenerateRefreshToken()
        {
            return (
                PasswordHasher.GenerateSecureToken(32),
                DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
            );
        }

        /// <summary>
        /// Validates a JWT token and returns the principal if valid.
        /// </summary>
        /// <param name="token">JWT token string</param>
        /// <returns>ClaimsPrincipal if token is valid; otherwise, null</returns>
        public ClaimsPrincipal ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var principal = tokenHandler.ValidateToken(
                    token,
                    _tokenValidationParameters,
                    out var validatedToken);

                return IsJwtWithValidSecurityAlgorithm(validatedToken) ? principal : null;
            }
            catch
            {
                return null;
            }
        }

        private bool IsJwtWithValidSecurityAlgorithm(SecurityToken validatedToken)
        {
            return (validatedToken is JwtSecurityToken jwtSecurityToken) &&
                   jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                       StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Extracts user ID from a validated JWT token.
        /// </summary>
        public int? GetUserIdFromToken(ClaimsPrincipal principal)
        {
            var userIdClaim = principal?.FindFirst("user_id") ?? principal?.FindFirst(JwtRegisteredClaimNames.Sub);

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                return userId;

            return null;
        }
    }
}
