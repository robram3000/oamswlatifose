using oamswlatifose.Server.DTO.Employee;
using FluentValidation;

namespace oamswlatifose.Server.Validations.Validators
{
    /// <summary>
    /// FluentValidation validator for CreateEmployeeDTO with comprehensive business rule validation.
    /// Ensures all employee creation requests meet business requirements and data integrity rules.
    /// </summary>
    public class CreateEmployeeValidator : AbstractValidator<CreateEmployeeDTO>
    {
        public CreateEmployeeValidator()
        {
            RuleFor(x => x.EmployeeID)
                .NotEmpty().WithMessage("Employee ID is required")
                .GreaterThan(0).WithMessage("Employee ID must be a positive number")
                .LessThan(1000000).WithMessage("Employee ID cannot exceed 1,000,000");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .Length(2, 100).WithMessage("First name must be between 2 and 100 characters")
                .Matches(@"^[a-zA-Z\s\-']+$").WithMessage("First name can only contain letters, spaces, hyphens, and apostrophes");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .Length(2, 100).WithMessage("Last name must be between 2 and 100 characters")
                .Matches(@"^[a-zA-Z\s\-']+$").WithMessage("Last name can only contain letters, spaces, hyphens, and apostrophes");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(255).WithMessage("Email cannot exceed 255 characters");

            RuleFor(x => x.Phone)
                .Matches(@"^\+?[0-9\s\-\(\)]{10,20}$").WithMessage("Invalid phone number format")
                .When(x => !string.IsNullOrEmpty(x.Phone));

            RuleFor(x => x.Position)
                .MaximumLength(100).WithMessage("Position cannot exceed 100 characters");

            RuleFor(x => x.Department)
                .MaximumLength(100).WithMessage("Department cannot exceed 100 characters");

            RuleFor(x => x.City)
                .MaximumLength(100).WithMessage("City cannot exceed 100 characters")
                .Matches(@"^[a-zA-Z\s\-]+$").WithMessage("City can only contain letters, spaces, and hyphens")
                .When(x => !string.IsNullOrEmpty(x.City));
        }
    }

    /// <summary>
    /// FluentValidation validator for UpdateEmployeeDTO with partial update support.
    /// </summary>
    public class UpdateEmployeeValidator : AbstractValidator<UpdateEmployeeDTO>
    {
        public UpdateEmployeeValidator()
        {
            RuleFor(x => x.FirstName)
                .Length(2, 100).WithMessage("First name must be between 2 and 100 characters")
                .Matches(@"^[a-zA-Z\s\-']+$").WithMessage("First name can only contain letters, spaces, hyphens, and apostrophes")
                .When(x => !string.IsNullOrEmpty(x.FirstName));

            RuleFor(x => x.LastName)
                .Length(2, 100).WithMessage("Last name must be between 2 and 100 characters")
                .Matches(@"^[a-zA-Z\s\-']+$").WithMessage("Last name can only contain letters, spaces, hyphens, and apostrophes")
                .When(x => !string.IsNullOrEmpty(x.LastName));

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(255).WithMessage("Email cannot exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.Email));

            RuleFor(x => x.Phone)
                .Matches(@"^\+?[0-9\s\-\(\)]{10,20}$").WithMessage("Invalid phone number format")
                .When(x => !string.IsNullOrEmpty(x.Phone));

            RuleFor(x => x.Position)
                .MaximumLength(100).WithMessage("Position cannot exceed 100 characters");

            RuleFor(x => x.Department)
                .MaximumLength(100).WithMessage("Department cannot exceed 100 characters");

            RuleFor(x => x.City)
                .MaximumLength(100).WithMessage("City cannot exceed 100 characters")
                .Matches(@"^[a-zA-Z\s\-]+$").WithMessage("City can only contain letters, spaces, and hyphens")
                .When(x => !string.IsNullOrEmpty(x.City));
        }
    }
}
