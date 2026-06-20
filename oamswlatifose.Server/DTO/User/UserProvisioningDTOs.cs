using System.ComponentModel.DataAnnotations;

namespace oamswlatifose.Server.DTO.User
{
    /// <summary>Admin/HR payload to create an employee + linked login account in one step.</summary>
    public class CreateUserAccountDTO
    {
        [Required(ErrorMessage = "First name is required")]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [MaxLength(100)]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "A valid email is required")]
        [MaxLength(255)]
        public string Email { get; set; }

        // Optional — nullable so ASP.NET doesn't treat the non-nullable string as implicitly required.
        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? Position { get; set; }

        [MaxLength(100)]
        public string? Department { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [MaxLength(100)]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Role is required")]
        public int RoleId { get; set; }
    }

    /// <summary>A user account row for the management list.</summary>
    public class UserAccountSummaryDTO
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string RoleName { get; set; }
        public int RoleId { get; set; }
        public int? EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        public bool IsActive { get; set; }
        public string CreatedAtFormatted { get; set; }
    }

    /// <summary>A role choice for the "add user" role dropdown.</summary>
    public class RoleOptionDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
