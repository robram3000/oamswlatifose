using oamswlatifose.Server.DTO.attendances;

namespace oamswlatifose.Server.Services.Attendance.Interfaces
{
    /// <summary>
    /// Drives the two-step, OTP-verified clock-in: an employee taps "Time In" (we capture the
    /// time and email a code), then submits that code to actually record the attendance —
    /// graded Present/Late against their work schedule.
    /// </summary>
    public interface IAttendanceVerificationService
    {
        /// <summary>
        /// Step 1 — capture the tap time + GPS, classify Office/Outside against the branch
        /// geofences, then issue + email an OTP for the clock-in.
        /// </summary>
        Task<ServiceResponse<AttendanceOtpRequestResultDTO>> RequestClockInOtpAsync(
            int employeeId, double? latitude, double? longitude, string clientIp);

        /// <summary>Step 2 — validate the OTP and write the schedule-graded attendance row.</summary>
        Task<ServiceResponse<AttendanceResponseDTO>> VerifyClockInAsync(
            int employeeId, string otpCode, string deviceInfo, string clientIp);

        /// <summary>Employee submits a clock-in request for HR/Admin to approve manually — no OTP email sent.</summary>
        Task<ServiceResponse<AttendanceOtpRequestResultDTO>> RequestAdminVerifyAsync(
            int employeeId, double? latitude, double? longitude, string clientIp);

        /// <summary>Returns all unexpired, unprocessed admin-verify requests (HR/Admin only).</summary>
        Task<ServiceResponse<List<PendingVerifyRequestDTO>>> GetPendingVerifyRequestsAsync();

        /// <summary>HR/Admin approves a pending request — records attendance with the employee's original tap time.</summary>
        Task<ServiceResponse<AttendanceResponseDTO>> ApproveVerifyRequestAsync(
            int requestId, string approverInfo, string clientIp);
    }
}
