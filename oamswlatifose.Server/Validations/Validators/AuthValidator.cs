using FluentValidation;
using oamswlatifose.Server.DTO.Auth;

namespace oamswlatifose.Server.Validations.Validators
{
    /// <summary>
    /// Validates login credentials. AuthenticationService requires IValidator&lt;LoginRequestDTO&gt;
    /// in its constructor, so this must exist for the service (and thus AuthController) to resolve.
    /// Picked up automatically by AddValidatorsFromAssemblyContaining&lt;Program&gt;().
    /// </summary>
    public class LoginRequestValidator : AbstractValidator<LoginRequestDTO>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required");
        }
    }

    /// <summary>Validates self-registration. Required for AuthenticationService to resolve.</summary>
    public class RegisterRequestValidator : AbstractValidator<RegisterRequestDTO>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("A valid email is required");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters");
        }
    }
}
