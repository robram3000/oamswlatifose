using System.ComponentModel.DataAnnotations;

namespace oamswlatifose.Server.DTO.Employee
{
    /// <summary>
    /// Data Transfer Object for detailed employee information returned to clients.
    /// Contains flattened representation of employee data with formatted fields
    /// and computed properties for UI display.
    /// </summary>
    public class EmployeeResponseDTO
    {
        public int Id { get; set; }
        public int EmployeeID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Position { get; set; }
        public string Department { get; set; }
        public string City { get; set; }
        public bool HasUserAccount { get; set; }
        public int? UserAccountId { get; set; }
        public int AttendanceCount { get; set; }
        public string CreatedAtFormatted { get; set; }
        public string UpdatedAtFormatted { get; set; }
    }

    /// <summary>
    /// Lightweight DTO for employee list views and dropdown selections.
    /// Contains only essential identification and organizational information.
    /// </summary>
    public class EmployeeSummaryDTO
    {
        public int Id { get; set; }
        public int EmployeeID { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }
    }

    /// <summary>
    /// DTO for creating new employee records.
    /// Contains required fields with validation attributes for API input validation.
    /// </summary>
    public class CreateEmployeeDTO
    {
        [Required(ErrorMessage = "Employee ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Employee ID must be a positive number")]
        public int EmployeeID { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters")]
        [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "First name can only contain letters, spaces, hyphens, and apostrophes")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters")]
        [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "Last name can only contain letters, spaces, hyphens, and apostrophes")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        public string Phone { get; set; }

        [StringLength(100, ErrorMessage = "Position cannot exceed 100 characters")]
        public string Position { get; set; }

        [StringLength(100, ErrorMessage = "Department cannot exceed 100 characters")]
        public string Department { get; set; }

        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters")]
        public string City { get; set; }
    }

    /// <summary>
    /// DTO for updating existing employee records.
    /// All properties are optional to support partial updates.
    /// </summary>
    public class UpdateEmployeeDTO
    {
        [StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters")]
        [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "First name can only contain letters, spaces, hyphens, and apostrophes")]
        public string FirstName { get; set; }

        [StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters")]
        [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "Last name can only contain letters, spaces, hyphens, and apostrophes")]
        public string LastName { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        public string Phone { get; set; }

        [StringLength(100, ErrorMessage = "Position cannot exceed 100 characters")]
        public string Position { get; set; }

        [StringLength(100, ErrorMessage = "Department cannot exceed 100 characters")]
        public string Department { get; set; }

        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters")]
        public string City { get; set; }
    }

    /// <summary>
    /// DTO for employee search and filtering parameters.
    /// </summary>
    public class EmployeeFilterDTO
    {
        public string SearchTerm { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }
        public bool? HasUserAccount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = "LastName";
        public string SortDirection { get; set; } = "ASC";
    }
}
