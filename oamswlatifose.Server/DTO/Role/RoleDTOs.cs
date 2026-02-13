using System.ComponentModel.DataAnnotations;

namespace oamswlatifose.Server.DTO.Role
{
    /// <summary>
    /// Detailed role response DTO with permission set and user count.
    /// </summary>
    public class RoleResponseDTO
    {
        public int Id { get; set; }
        public string RoleName { get; set; }
        public string Description { get; set; }
        public int UserCount { get; set; }
        public Dictionary<string, bool> Permissions { get; set; }
        public List<string> PermissionSummary { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedAtFormatted { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedAtFormatted { get; set; }
    }

    /// <summary>
    /// Summary DTO for role dropdowns and list views.
    /// </summary>
    public class RoleSummaryDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int UserCount { get; set; }
        public bool IsAssignable { get; set; }
    }

    /// <summary>
    /// DTO for creating new roles.
    /// </summary>
    public class CreateRoleDTO
    {
        [Required(ErrorMessage = "Role name is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Role name must be between 3 and 100 characters")]
        [RegularExpression(@"^[a-zA-Z0-9\s-]+$", ErrorMessage = "Role name can only contain letters, numbers, spaces, and hyphens")]
        public string RoleName { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; }

        public bool CanViewEmployees { get; set; }
        public bool CanEditEmployees { get; set; }
        public bool CanDeleteEmployees { get; set; }
        public bool CanViewAttendance { get; set; }
        public bool CanEditAttendance { get; set; }
        public bool CanGenerateReports { get; set; }
        public bool CanManageUsers { get; set; }
        public bool CanManageRoles { get; set; }
        public bool CanAccessAdminPanel { get; set; }
    }

    /// <summary>
    /// DTO for updating existing roles (excluding role name).
    /// </summary>
    public class UpdateRoleDTO
    {
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; }

        public bool? CanViewEmployees { get; set; }
        public bool? CanEditEmployees { get; set; }
        public bool? CanDeleteEmployees { get; set; }
        public bool? CanViewAttendance { get; set; }
        public bool? CanEditAttendance { get; set; }
        public bool? CanGenerateReports { get; set; }
        public bool? CanManageUsers { get; set; }
        public bool? CanManageRoles { get; set; }
        public bool? CanAccessAdminPanel { get; set; }
        public bool? IsActive { get; set; }
    }

    /// <summary>
    /// DTO for updating role permissions only.
    /// </summary>
    public class UpdateRolePermissionsDTO
    {
        public Dictionary<string, bool> Permissions { get; set; }
    }

    /// <summary>
    /// DTO for assigning role to user.
    /// </summary>
    public class AssignRoleDTO
    {
        [Required(ErrorMessage = "User ID is required")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Role ID is required")]
        public int RoleId { get; set; }
    }
}
