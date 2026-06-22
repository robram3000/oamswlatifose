using System.ComponentModel.DataAnnotations;

namespace oamswlatifose.Server.DTO.attendances
{
    /// <summary>Body for requesting a clock-in OTP — carries the employee's GPS and client tap time (optional).</summary>
    public class RequestClockInOtpDTO
    {
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        /// <summary>Unix epoch ms captured on the client at the moment the button was tapped — used to record the accurate tap time even if location lookup adds delay.</summary>
        public long? ClientTimestampMs { get; set; }
    }

    /// <summary>Body for requesting a clock-out OTP — carries the client tap time (optional).</summary>
    public class RequestClockOutOtpDTO
    {
        /// <summary>Unix epoch ms captured on the client at the moment the Time Out button was tapped.</summary>
        public long? ClientTimestampMs { get; set; }
    }

    /// <summary>Result of requesting an attendance OTP — what the client needs to drive the verify step.</summary>
    public class AttendanceOtpRequestResultDTO
    {
        public bool Sent { get; set; }
        public string Message { get; set; }
        public string EmailMasked { get; set; }
        public int ExpiresInMinutes { get; set; }
        public DateTime? ExpiresAt { get; set; }

        /// <summary>The time-of-day captured at request, "hh:mm tt" — shown in the verify dialog.</summary>
        public string RequestedTimeFormatted { get; set; }

        // Geofence resolution for this request.
        public string WorkLocation { get; set; }   // Office / Outside / Unknown
        public string BranchName { get; set; }
        public bool OnSite { get; set; }
        public int? DistanceMeters { get; set; }
    }

    /// <summary>Body for verifying an attendance OTP and committing the clock-in/out.</summary>
    public class VerifyAttendanceOtpDTO
    {
        [Required(ErrorMessage = "Verification code is required")]
        [RegularExpression(@"^\d{4,8}$", ErrorMessage = "Code must be 4–8 digits")]
        public string OtpCode { get; set; }
    }

    /// <summary>A pending admin-verify clock-in request visible to HR/Admin.</summary>
    public class PendingVerifyRequestDTO
    {
        public int RequestId { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        public string RequestedTimeFormatted { get; set; }
        public string WorkLocation { get; set; }
        public string BranchName { get; set; }
        public bool OnSite { get; set; }
        public DateTime RequestedAt { get; set; }
    }
}
