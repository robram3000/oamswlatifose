using oamswlatifose.Server.DTO.Role;

namespace oamswlatifose.Server.Validations.Validators
{
    /// <summary>
    /// FluentValidation validator for CreateRoleDTO.
    /// </summary>
    public class CreateRoleValidator : AbstractValidator<CreateRoleDTO>
    {
        public CreateRoleValidator()
        {
            RuleFor(x => x.RoleName)
                .NotEmpty().WithMessage("Role name is required")
                .Length(3, 100).WithMessage("Role name must be between 3 and 100 characters")
                .Matches(@"^[a-zA-Z0-9\s-]+$").WithMessage("Role name can only contain letters, numbers, spaces, and hyphens");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

            // At least one permission should be granted
            RuleFor(x => x)
                .Must(x => x.CanViewEmployees || x.CanEditEmployees || x.CanDeleteEmployees ||
                          x.CanViewAttendance || x.CanEditAttendance || x.CanGenerateReports ||
                          x.CanManageUsers || x.CanManageRoles || x.CanAccessAdminPanel)
                .WithMessage("At least one permission must be granted");
        }
    }

    /// <summary>
    /// FluentValidation validator for UpdateRoleDTO.
    /// </summary>
    public class UpdateRoleValidator : AbstractValidator<UpdateRoleDTO>
    {
        public UpdateRoleValidator()
        {
            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
                .When(x => !string.IsNullOrEmpty(x.Description));

            // Ensure role doesn't become completely permission-less if all permissions are explicitly set to false
            RuleFor(x => x)
                .Must(x => !(x.CanViewEmployees == false && x.CanEditEmployees == false &&
                           x.CanDeleteEmployees == false && x.CanViewAttendance == false &&
                           x.CanEditAttendance == false && x.CanGenerateReports == false &&
                           x.CanManageUsers == false && x.CanManageRoles == false &&
                           x.CanAccessAdminPanel == false))
                .WithMessage("Role cannot have all permissions disabled")
                .When(x => x.CanViewEmployees.HasValue || x.CanEditEmployees.HasValue ||
                          x.CanDeleteEmployees.HasValue || x.CanViewAttendance.HasValue ||
                          x.CanEditAttendance.HasValue || x.CanGenerateReports.HasValue ||
                          x.CanManageUsers.HasValue || x.CanManageRoles.HasValue ||
                          x.CanAccessAdminPanel.HasValue);
        }
    }

    /// <summary>
    /// FluentValidation validator for AssignRoleDTO.
    /// </summary>
    public class AssignRoleValidator : AbstractValidator<AssignRoleDTO>
    {
        public AssignRoleValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required")
                .GreaterThan(0).WithMessage("Invalid user ID");

            RuleFor(x => x.RoleId)
                .NotEmpty().WithMessage("Role ID is required")
                .GreaterThan(0).WithMessage("Invalid role ID");
        }
    }
}
