using AutoMapper;
using oamswlatifose.Server.Model.security;

namespace oamswlatifose.Server.MappingProfiles
{
    /// <summary>
    /// AutoMapper profile for Role-Based Access Control entity mappings.
    /// Handles transformation of role definitions and permission sets for
    /// authorization decisions and role management interfaces.
    /// 
    /// <para>Mapping Features:</para>
    /// <para>- Permission set flattening for easy client consumption</para>
    /// <para>- User count aggregation for role popularity metrics</para>
    /// <para>- Permission dictionary conversion for dynamic UI rendering</para>
    /// <para>- Role status determination with active/inactive states</para>
    /// </summary>
    public class RoleMappingProfile : Profile
    {
        public RoleMappingProfile()
        {
            // Map Role entity to detailed response DTO with permission summary
            CreateMap<EMRoleBasedAccessControl, RoleResponseDTO>()
                .ForMember(dest => dest.UserCount,
                    opt => opt.MapFrom(src => src.Users != null ? src.Users.Count : 0))
                .ForMember(dest => dest.Permissions,
                    opt => opt.MapFrom(src => MapPermissionsToDictionary(src)))
                .ForMember(dest => dest.PermissionSummary,
                    opt => opt.MapFrom(src => GetPermissionSummary(src)))
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => src.IsActive ? "Active" : "Inactive"))
                .ForMember(dest => dest.CreatedAtFormatted,
                    opt => opt.MapFrom(src => src.CreatedAt.ToString("yyyy-MM-dd HH:mm")))
                .ForMember(dest => dest.UpdatedAtFormatted,
                    opt => opt.MapFrom(src => src.UpdatedAt.HasValue
                        ? src.UpdatedAt.Value.ToString("yyyy-MM-dd HH:mm")
                        : null));

            // Map Role entity to lightweight summary DTO for dropdowns
            CreateMap<EMRoleBasedAccessControl, RoleSummaryDTO>()
                .ForMember(dest => dest.Name,
                    opt => opt.MapFrom(src => src.RoleName))
                .ForMember(dest => dest.UserCount,
                    opt => opt.MapFrom(src => src.Users != null ? src.Users.Count : 0))
                .ForMember(dest => dest.IsAssignable,
                    opt => opt.MapFrom(src => src.IsActive));

            // Map create role request to Role entity
            CreateMap<CreateRoleDTO, EMRoleBasedAccessControl>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Users, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));

            // Map update role request to Role entity
            CreateMap<UpdateRoleDTO, EMRoleBasedAccessControl>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.RoleName, opt => opt.Ignore()) // Prevent renaming
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Users, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Map permission update request to Role entity
            CreateMap<UpdateRolePermissionsDTO, EMRoleBasedAccessControl>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.RoleName, opt => opt.Ignore())
                .ForMember(dest => dest.Description, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.Users, opt => opt.Ignore());
        }

        private Dictionary<string, bool> MapPermissionsToDictionary(EMRoleBasedAccessControl role)
        {
            return new Dictionary<string, bool>
            {
                ["viewEmployees"] = role.CanViewEmployees,
                ["editEmployees"] = role.CanEditEmployees,
                ["deleteEmployees"] = role.CanDeleteEmployees,
                ["viewAttendance"] = role.CanViewAttendance,
                ["editAttendance"] = role.CanEditAttendance,
                ["generateReports"] = role.CanGenerateReports,
                ["manageUsers"] = role.CanManageUsers,
                ["manageRoles"] = role.CanManageRoles,
                ["accessAdmin"] = role.CanAccessAdminPanel
            };
        }

        private List<string> GetPermissionSummary(EMRoleBasedAccessControl role)
        {
            var permissions = new List<string>();

            if (role.CanViewEmployees) permissions.Add("View Employees");
            if (role.CanEditEmployees) permissions.Add("Edit Employees");
            if (role.CanDeleteEmployees) permissions.Add("Delete Employees");
            if (role.CanViewAttendance) permissions.Add("View Attendance");
            if (role.CanEditAttendance) permissions.Add("Edit Attendance");
            if (role.CanGenerateReports) permissions.Add("Generate Reports");
            if (role.CanManageUsers) permissions.Add("Manage Users");
            if (role.CanManageRoles) permissions.Add("Manage Roles");
            if (role.CanAccessAdminPanel) permissions.Add("Admin Access");

            return permissions;
        }
    }
}
