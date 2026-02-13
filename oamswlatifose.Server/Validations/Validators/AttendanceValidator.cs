using oamswlatifose.Server.DTO.attendances;

namespace oamswlatifose.Server.Validations.Validators
{
    /// <summary>
    /// FluentValidation validator for CreateAttendanceDTO with time tracking business rules.
    /// </summary>
    public class CreateAttendanceValidator : AbstractValidator<CreateAttendanceDTO>
    {
        public CreateAttendanceValidator()
        {
            RuleFor(x => x.EmployeeId)
                .NotEmpty().WithMessage("Employee ID is required")
                .GreaterThan(0).WithMessage("Invalid employee ID");

            RuleFor(x => x.AttendanceDate)
                .NotEmpty().WithMessage("Attendance date is required")
                .LessThanOrEqualTo(DateTime.Today).WithMessage("Attendance date cannot be in the future");

            RuleFor(x => x.TimeIn)
                .Must((dto, timeIn) => !timeIn.HasValue || timeIn.Value < dto.TimeOut)
                .WithMessage("Time in must be before time out")
                .When(x => x.TimeIn.HasValue && x.TimeOut.HasValue);

            RuleFor(x => x.TimeOut)
                .Must((dto, timeOut) => !timeOut.HasValue || timeOut.Value > dto.TimeIn)
                .WithMessage("Time out must be after time in")
                .When(x => x.TimeIn.HasValue && x.TimeOut.HasValue);

            RuleFor(x => x.Status)
                .MaximumLength(50).WithMessage("Status cannot exceed 50 characters")
                .Must(status => new[] { "Present", "Absent", "Late", "Half-Day", "Holiday", "Leave" }.Contains(status))
                .WithMessage("Invalid attendance status")
                .When(x => !string.IsNullOrEmpty(x.Status));

            RuleFor(x => x.Shift)
                .MaximumLength(10).WithMessage("Shift cannot exceed 10 characters")
                .Must(shift => new[] { "Morning", "Afternoon", "Evening", "Night" }.Contains(shift))
                .WithMessage("Invalid shift value")
                .When(x => !string.IsNullOrEmpty(x.Shift));

            RuleFor(x => x.Remarks)
                .MaximumLength(500).WithMessage("Remarks cannot exceed 500 characters");
        }
    }

    /// <summary>
    /// FluentValidation validator for ClockInDTO with geolocation validation.
    /// </summary>
    public class ClockInValidator : AbstractValidator<ClockInDTO>
    {
        public ClockInValidator()
        {
            RuleFor(x => x.EmployeeId)
                .NotEmpty().WithMessage("Employee ID is required")
                .GreaterThan(0).WithMessage("Invalid employee ID");

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90).WithMessage("Invalid latitude value")
                .When(x => x.Latitude.HasValue);

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180).WithMessage("Invalid longitude value")
                .When(x => x.Longitude.HasValue);

            RuleFor(x => x.DeviceInfo)
                .MaximumLength(500).WithMessage("Device info cannot exceed 500 characters");
        }
    }

    /// <summary>
    /// FluentValidation validator for ClockOutDTO.
    /// </summary>
    public class ClockOutValidator : AbstractValidator<ClockOutDTO>
    {
        public ClockOutValidator()
        {
            RuleFor(x => x.EmployeeId)
                .NotEmpty().WithMessage("Employee ID is required")
                .GreaterThan(0).WithMessage("Invalid employee ID");

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90).WithMessage("Invalid latitude value")
                .When(x => x.Latitude.HasValue);

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180).WithMessage("Invalid longitude value")
                .When(x => x.Longitude.HasValue);
        }
    }

    /// <summary>
    /// FluentValidation validator for AttendanceReportDTO.
    /// </summary>
    public class AttendanceReportValidator : AbstractValidator<AttendanceReportDTO>
    {
        public AttendanceReportValidator()
        {
            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage("Start date is required");

            RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage("End date is required")
                .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("End date must be after or equal to start date");

            RuleFor(x => x.EmployeeId)
                .GreaterThan(0).WithMessage("Invalid employee ID")
                .When(x => x.EmployeeId.HasValue);

            RuleFor(x => x.Department)
                .MaximumLength(100).WithMessage("Department filter cannot exceed 100 characters");

            RuleFor(x => x.Status)
                .MaximumLength(50).WithMessage("Status filter cannot exceed 50 characters");

            RuleFor(x => x.ReportFormat)
                .Must(format => new[] { "JSON", "PDF", "EXCEL", "CSV" }.Contains(format))
                .WithMessage("Invalid report format");
        }
    }
}
