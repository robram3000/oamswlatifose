using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace oamswlatifose.Server.Validations.Attributes
{
    /// <summary>
    /// Validates that a date is not in the past.
    /// Used for attendance dates, scheduled events, and future-dated operations.
    /// </summary>
    public class FutureDateAttribute : ValidationAttribute
    {
        private readonly bool _allowToday;

        public FutureDateAttribute(bool allowToday = true)
        {
            _allowToday = allowToday;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            if (value is DateTime date)
            {
                var today = DateTime.Today;
                var compareDate = date.Date;

                if (_allowToday && compareDate == today)
                    return ValidationResult.Success;

                if (compareDate > today)
                    return ValidationResult.Success;

                return new ValidationResult(ErrorMessage ?? "Date must be in the future");
            }

            return new ValidationResult("Invalid date format");
        }
    }

    /// <summary>
    /// Validates that a date is not in the future.
    /// Used for birth dates, hire dates, and historical records.
    /// </summary>
    public class PastDateAttribute : ValidationAttribute
    {
        private readonly bool _allowToday;

        public PastDateAttribute(bool allowToday = true)
        {
            _allowToday = allowToday;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            if (value is DateTime date)
            {
                var today = DateTime.Today;
                var compareDate = date.Date;

                if (_allowToday && compareDate == today)
                    return ValidationResult.Success;

                if (compareDate < today)
                    return ValidationResult.Success;

                return new ValidationResult(ErrorMessage ?? "Date must be in the past");
            }

            return new ValidationResult("Invalid date format");
        }
    }

    /// <summary>
    /// Validates that one date is greater than another date.
    /// Used for date ranges, employment periods, and project timelines.
    /// </summary>
    public class DateGreaterThanAttribute : ValidationAttribute
    {
        private readonly string _comparisonProperty;

        public DateGreaterThanAttribute(string comparisonProperty)
        {
            _comparisonProperty = comparisonProperty;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var currentValue = value as DateTime?;
            var comparisonProperty = validationContext.ObjectType.GetProperty(_comparisonProperty);

            if (comparisonProperty == null)
                throw new ArgumentException($"Property {_comparisonProperty} not found");

            var comparisonValue = comparisonProperty.GetValue(validationContext.ObjectInstance) as DateTime?;

            if (!currentValue.HasValue || !comparisonValue.HasValue)
                return ValidationResult.Success;

            if (currentValue.Value > comparisonValue.Value)
                return ValidationResult.Success;

            return new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} must be greater than {_comparisonProperty}");
        }
    }

    /// <summary>
    /// Validates that a string contains only alphanumeric characters and optional special characters.
    /// </summary>
    public class AlphanumericAttribute : ValidationAttribute
    {
        private readonly string _additionalChars;

        public AlphanumericAttribute(string additionalChars = "")
        {
            _additionalChars = additionalChars;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return ValidationResult.Success;

            var pattern = $"^[a-zA-Z0-9{Regex.Escape(_additionalChars)}]+$";
            if (Regex.IsMatch(value.ToString(), pattern))
                return ValidationResult.Success;

            return new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} can only contain alphanumeric characters");
        }
    }

    /// <summary>
    /// Validates that a string is a valid phone number format.
    /// </summary>
    public class ValidPhoneNumberAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return ValidationResult.Success;

            var phoneNumber = value.ToString().Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

            if (Regex.IsMatch(phoneNumber, @"^\+?[0-9]{10,15}$"))
                return ValidationResult.Success;

            return new ValidationResult(ErrorMessage ?? "Invalid phone number format");
        }
    }

    /// <summary>
    /// Validates that a time value is within business hours.
    /// </summary>
    public class BusinessHoursAttribute : ValidationAttribute
    {
        private readonly TimeSpan _startTime;
        private readonly TimeSpan _endTime;

        public BusinessHoursAttribute(string startTime = "08:00", string endTime = "18:00")
        {
            _startTime = TimeSpan.Parse(startTime);
            _endTime = TimeSpan.Parse(endTime);
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            if (value is TimeSpan time)
            {
                if (time >= _startTime && time <= _endTime)
                    return ValidationResult.Success;

                return new ValidationResult(ErrorMessage ?? $"Time must be between {_startTime:hh\\:mm} and {_endTime:hh\\:mm}");
            }

            return new ValidationResult("Invalid time format");
        }
    }

    /// <summary>
    /// Validates that an email domain is from an allowed list.
    /// </summary>
    public class AllowedEmailDomainAttribute : ValidationAttribute
    {
        private readonly string[] _allowedDomains;

        public AllowedEmailDomainAttribute(params string[] allowedDomains)
        {
            _allowedDomains = allowedDomains;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return ValidationResult.Success;

            var email = value.ToString();
            var domain = email.Split('@').LastOrDefault();

            if (_allowedDomains.Contains(domain, StringComparer.OrdinalIgnoreCase))
                return ValidationResult.Success;

            return new ValidationResult(ErrorMessage ?? $"Email domain must be one of: {string.Join(", ", _allowedDomains)}");
        }
    }
}
