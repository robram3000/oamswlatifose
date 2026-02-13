using AutoMapper;
using oamswlatifose.Server.Model.security;

namespace oamswlatifose.Server.MappingProfiles
{
    /// <summary>
    /// AutoMapper profile for User account and authentication entity mappings.
    /// Handles secure transformation of sensitive user data with explicit exclusion
    /// of password hashes, salts, and security tokens from API responses.
    /// 
    /// <para>Security Features:</para>
    /// <para>- Password hashes and salts are NEVER mapped to response DTOs</para>
    /// <para>- Reset tokens are excluded from standard user responses</para>
    /// <para>- Role permissions are flattened for easy client consumption</para>
    /// <para>- Employee data is selectively exposed based on privacy requirements</para>
    /// </summary>
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            // Map User entity to detailed response DTO with security-sensitive fields excluded
            CreateMap<EMAuthorizeruser, UserResponseDTO>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordSalt, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordResetToken, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordResetTokenExpires, opt => opt.Ignore())
                .ForMember(dest => dest.RoleName,
                    opt => opt.MapFrom(src => src.Role != null ? src.Role.RoleName : null))
                .ForMember(dest => dest.RolePermissions,
                    opt => opt.MapFrom(src => src.Role != null ? MapRolePermissions(src.Role) : null))
                .ForMember(dest => dest.EmployeeName,
                    opt => opt.MapFrom(src => src.Employee != null ? $"{src.Employee.FirstName} {src.Employee.LastName}" : null))
                .ForMember(dest => dest.IsLocked,
                    opt => opt.MapFrom(src => src.LockoutEnd.HasValue && src.LockoutEnd > DateTime.UtcNow))
                .ForMember(dest => dest.LockoutRemainingMinutes,
                    opt => opt.MapFrom(src => src.LockoutEnd.HasValue && src.LockoutEnd > DateTime.UtcNow
                        ? (int)(src.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes
                        : 0))
                .ForMember(dest => dest.AccountStatus,
                    opt => opt.MapFrom(src => GetAccountStatus(src)))
                .ForMember(dest => dest.LastLoginFormatted,
                    opt => opt.MapFrom(src => src.LastLogin.HasValue ? src.LastLogin.Value.ToString("yyyy-MM-dd HH:mm") : "Never"))
                .ForMember(dest => dest.CreatedAtFormatted,
                    opt => opt.MapFrom(src => src.CreatedAt.ToString("yyyy-MM-dd HH:mm")));

            // Map User entity to lightweight summary DTO
            CreateMap<EMAuthorizeruser, UserSummaryDTO>()
                .ForMember(dest => dest.RoleName,
                    opt => opt.MapFrom(src => src.Role != null ? src.Role.RoleName : null))
                .ForMember(dest => dest.IsActive,
                    opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.IsVerified,
                    opt => opt.MapFrom(src => src.IsEmailVerified));

            // Map create user request to User entity
            CreateMap<CreateUserDTO, EMAuthorizeruser>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordSalt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.LastLogin, opt => opt.Ignore())
                .ForMember(dest => dest.FailedLoginAttempts, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.IsEmailVerified, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.Sessions, opt => opt.Ignore())
                .ForMember(dest => dest.AuthLogs, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.Ignore())
                .ForMember(dest => dest.Employee, opt => opt.Ignore());

            // Map update user request to User entity
            CreateMap<UpdateUserDTO, EMAuthorizeruser>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Username, opt => opt.Ignore()) // Username cannot be changed
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordSalt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.FailedLoginAttempts, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEnd, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordResetToken, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordResetTokenExpires, opt => opt.Ignore())
                .ForMember(dest => dest.Sessions, opt => opt.Ignore())
                .ForMember(dest => dest.AuthLogs, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.Ignore())
                .ForMember(dest => dest.Employee, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Map login request to authentication context
            CreateMap<LoginRequestDTO, AuthContextDTO>()
                .ForMember(dest => dest.LoginTime, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.CorrelationId, opt => opt.Ignore());

            // Map registration request to User entity
            CreateMap<RegisterRequestDTO, EMAuthorizeruser>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordSalt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.IsEmailVerified, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.FailedLoginAttempts, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.RoleId, opt => opt.MapFrom(src => 2)); // Default role ID for employees
        }

        private Dictionary<string, bool> MapRolePermissions(EMRoleBasedAccessControl role)
        {
            if (role == null) return null;

            return new Dictionary<string, bool>
            {
                ["CanViewEmployees"] = role.CanViewEmployees,
                ["CanEditEmployees"] = role.CanEditEmployees,
                ["CanDeleteEmployees"] = role.CanDeleteEmployees,
                ["CanViewAttendance"] = role.CanViewAttendance,
                ["CanEditAttendance"] = role.CanEditAttendance,
                ["CanGenerateReports"] = role.CanGenerateReports,
                ["CanManageUsers"] = role.CanManageUsers,
                ["CanManageRoles"] = role.CanManageRoles,
                ["CanAccessAdminPanel"] = role.CanAccessAdminPanel
            };
        }

        private string GetAccountStatus(EMAuthorizeruser user)
        {
            if (!user.IsActive) return "Inactive";
            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow) return "Locked";
            if (!user.IsEmailVerified) return "Pending Verification";
            return "Active";
        }
    }
}
