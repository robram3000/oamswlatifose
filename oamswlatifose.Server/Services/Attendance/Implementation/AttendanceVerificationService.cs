using AutoMapper;
using Microsoft.EntityFrameworkCore;
using oamswlatifose.Server.Auth;
using oamswlatifose.Server.DTO.attendances;
using oamswlatifose.Server.Model;
using oamswlatifose.Server.Model.occurance;
using oamswlatifose.Server.Services.Attendance.Interfaces;
using oamswlatifose.Server.Services.Branch.Interfaces;
using oamswlatifose.Server.Services.Email.Interfaces;
using oamswlatifose.Server.Services.Schedule.Interfaces;

namespace oamswlatifose.Server.Services.Attendance.Implementation
{
    /// <summary>
    /// Implements the OTP-verified clock-in. Codes are emailed (real SMTP) and stored in
    /// EMAttendanceOtp; the attendance row is only written after a valid code, using the
    /// time captured at request so the recorded time-in reflects the actual tap.
    /// </summary>
    public class AttendanceVerificationService : IAttendanceVerificationService
    {
        private const string Purpose = "ClockIn";
        private const int OtpLength = 6;
        private const int OtpExpiryMinutes = 10;
        private const int MaxAttempts = 3;

        private readonly ApplicationDbContext _db;
        private readonly IEmailService _emailService;
        private readonly IOTPGenerator _otpGenerator;
        private readonly IWorkScheduleService _scheduleService;
        private readonly IBranchService _branchService;
        private readonly IMapper _mapper;
        private readonly ILogger<AttendanceVerificationService> _logger;

        public AttendanceVerificationService(
            ApplicationDbContext db,
            IEmailService emailService,
            IOTPGenerator otpGenerator,
            IWorkScheduleService scheduleService,
            IBranchService branchService,
            IMapper mapper,
            ILogger<AttendanceVerificationService> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _otpGenerator = otpGenerator ?? throw new ArgumentNullException(nameof(otpGenerator));
            _scheduleService = scheduleService ?? throw new ArgumentNullException(nameof(scheduleService));
            _branchService = branchService ?? throw new ArgumentNullException(nameof(branchService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ServiceResponse<AttendanceOtpRequestResultDTO>> RequestClockInOtpAsync(
            int employeeId, double? latitude, double? longitude, string clientIp)
        {
            try
            {
                var employee = await _db.EMEmployees.FirstOrDefaultAsync(e => e.Id == employeeId);
                if (employee == null)
                    return ServiceResponse<AttendanceOtpRequestResultDTO>.FailureResult("No employee record linked to your account");

                if (string.IsNullOrWhiteSpace(employee.Email))
                    return ServiceResponse<AttendanceOtpRequestResultDTO>.FailureResult("Your employee record has no email to send the code to");

                var today = DateTime.Today;
                var todays = await _db.EMAttendance
                    .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.AttendanceDate == today);
                if (todays != null && todays.TimeIn.HasValue)
                    return ServiceResponse<AttendanceOtpRequestResultDTO>.FailureResult("You have already clocked in today");

                // Block clock-in when HR/Admin has marked today as Closed.
                var closedEvent = await _db.EMWorkEvents
                    .FirstOrDefaultAsync(e => e.Date == today && e.EventType == "Closed");
                if (closedEvent != null)
                    return ServiceResponse<AttendanceOtpRequestResultDTO>.FailureResult(
                        $"Attendance is closed for today ({closedEvent.Name}). Contact HR if this is an error.");

                // Classify the GPS point against the branch geofences (Office vs Outside).
                var loc = await _branchService.ResolveAsync(latitude, longitude);

                // Optional enforcement: block off-site clock-ins when configured to office-only.
                if (_branchService.RequireOnSite && !loc.OnSite)
                {
                    var why = loc.WorkLocation == "Unknown"
                        ? "Location is required to clock in. Please enable location access and try again."
                        : "You must be within an office branch to clock in.";
                    return ServiceResponse<AttendanceOtpRequestResultDTO>.FailureResult(why);
                }

                // Keep a single active code: retire any prior unused clock-in OTPs.
                var stale = await _db.EMAttendanceOtps
                    .Where(o => o.EmployeeId == employeeId && o.Purpose == Purpose && !o.IsUsed)
                    .ToListAsync();
                foreach (var s in stale) s.IsUsed = true;

                var (code, expiry) = _otpGenerator.GenerateOTPWithExpiry(OtpLength, OtpExpiryMinutes);
                var requestedTime = DateTime.Now.TimeOfDay;

                _db.EMAttendanceOtps.Add(new EMAttendanceOtp
                {
                    EmployeeId = employeeId,
                    Email = employee.Email,
                    Code = code,
                    Purpose = Purpose,
                    RequestedTime = requestedTime,
                    Latitude = latitude,
                    Longitude = longitude,
                    WorkLocation = loc.WorkLocation,
                    BranchId = loc.BranchId,
                    ExpiresAt = expiry,
                    IsUsed = false,
                    Attempts = 0,
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();

                var name = $"{employee.FirstName} {employee.LastName}".Trim();
                var emailTo = employee.Email;
                var emailBody = BuildOtpEmail(name, code, OtpExpiryMinutes);

                // Await the send so that: (a) the request scope is still alive, avoiding
                // ObjectDisposedException on the log repository, and (b) SMTP errors surface
                // to the caller instead of being silently swallowed in a fire-and-forget task.
                var emailResult = await _emailService.SendHtmlEmailAsync(
                    emailTo, "Your attendance clock-in code", emailBody);

                if (!emailResult.IsSuccess)
                    _logger.LogWarning("OTP email failed for employee {EmployeeId}: {Msg}", employeeId, emailResult.Message);
                else
                    _logger.LogInformation("Clock-in OTP issued for employee {EmployeeId} from {Ip}", employeeId, clientIp);

                return ServiceResponse<AttendanceOtpRequestResultDTO>.SuccessResult(new AttendanceOtpRequestResultDTO
                {
                    Sent = emailResult.IsSuccess,
                    Message = emailResult.IsSuccess
                        ? "Verification code sent to your email"
                        : $"Could not send the code — {emailResult.Message}",
                    EmailMasked = MaskEmail(employee.Email),
                    ExpiresInMinutes = OtpExpiryMinutes,
                    ExpiresAt = expiry,
                    RequestedTimeFormatted = DateTime.Today.Add(requestedTime).ToString("hh:mm tt"),
                    WorkLocation = loc.WorkLocation,
                    BranchName = loc.BranchName,
                    OnSite = loc.OnSite,
                    DistanceMeters = loc.DistanceMeters
                }, emailResult.IsSuccess ? "Verification code sent" : "Email delivery failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting clock-in OTP for employee {EmployeeId}", employeeId);
                return ServiceResponse<AttendanceOtpRequestResultDTO>.FromException(ex, "Failed to send verification code");
            }
        }

        public async Task<ServiceResponse<AttendanceResponseDTO>> VerifyClockInAsync(
            int employeeId, string otpCode, string deviceInfo, string clientIp)
        {
            try
            {
                var otp = await _db.EMAttendanceOtps
                    .Where(o => o.EmployeeId == employeeId && o.Purpose == Purpose && !o.IsUsed)
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefaultAsync();

                if (otp == null)
                    return ServiceResponse<AttendanceResponseDTO>.FailureResult("No pending verification. Please tap Time In to get a new code.");

                if (otp.ExpiresAt < DateTime.UtcNow)
                {
                    otp.IsUsed = true;
                    await _db.SaveChangesAsync();
                    return ServiceResponse<AttendanceResponseDTO>.FailureResult("Your code has expired. Please request a new one.");
                }

                if (otp.Attempts >= MaxAttempts)
                {
                    otp.IsUsed = true;
                    await _db.SaveChangesAsync();
                    return ServiceResponse<AttendanceResponseDTO>.FailureResult("Too many incorrect attempts. Please request a new code.");
                }

                if (otp.Code != otpCode?.Trim())
                {
                    otp.Attempts++;
                    await _db.SaveChangesAsync();
                    var left = Math.Max(0, MaxAttempts - otp.Attempts);
                    return ServiceResponse<AttendanceResponseDTO>.FailureResult(
                        $"Incorrect code. {left} attempt{(left == 1 ? "" : "s")} remaining.");
                }

                otp.IsUsed = true;

                var today = DateTime.Today;
                var existing = await _db.EMAttendance
                    .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.AttendanceDate == today);
                if (existing != null && existing.TimeIn.HasValue)
                {
                    await _db.SaveChangesAsync();
                    return ServiceResponse<AttendanceResponseDTO>.FailureResult("You have already clocked in today");
                }

                var schedule = await _scheduleService.GetEntityAsync(employeeId);
                var status = _scheduleService.ComputeStatus(schedule, otp.RequestedTime);

                var locationNote = otp.WorkLocation == "Office" ? "Office" : otp.WorkLocation == "Outside" ? "Off-site" : "Unknown location";

                EMAttendance attendance;
                if (existing != null)
                {
                    // Fill in an existing same-day row (e.g. an admin-created "Absent" placeholder).
                    existing.TimeIn = otp.RequestedTime;
                    existing.Status = status;
                    existing.Shift = DetermineShift(otp.RequestedTime);
                    existing.WorkLocation = otp.WorkLocation;
                    existing.BranchId = otp.BranchId;
                    existing.Latitude = otp.Latitude;
                    existing.Longitude = otp.Longitude;
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.Remarks = $"Clock-in (OTP verified, {locationNote}) via {deviceInfo ?? "Unknown device"}";
                    attendance = existing;
                }
                else
                {
                    attendance = new EMAttendance
                    {
                        EmployeeId = employeeId,
                        AttendanceDate = today,
                        TimeIn = otp.RequestedTime,
                        Status = status,
                        Shift = DetermineShift(otp.RequestedTime),
                        WorkLocation = otp.WorkLocation,
                        BranchId = otp.BranchId,
                        Latitude = otp.Latitude,
                        Longitude = otp.Longitude,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Remarks = $"Clock-in (OTP verified, {locationNote}) via {deviceInfo ?? "Unknown device"}"
                    };
                    _db.EMAttendance.Add(attendance);
                }

                await _db.SaveChangesAsync();

                // Load the employee for name/department flattening in the mapper.
                attendance.Employee ??= await _db.EMEmployees.FirstOrDefaultAsync(e => e.Id == employeeId);

                _logger.LogInformation("Employee {EmployeeId} clocked in (OTP) at {Time} → {Status} ({Loc})",
                    employeeId, otp.RequestedTime, status, otp.WorkLocation);

                var dto = _mapper.Map<AttendanceResponseDTO>(attendance);
                // BranchName isn't on the entity — resolve it for the immediate response.
                if (otp.BranchId.HasValue)
                    dto.BranchName = (await _db.EMBranches.FirstOrDefaultAsync(b => b.Id == otp.BranchId))?.Name;

                var locSuffix = otp.WorkLocation == "Office" && dto.BranchName != null ? $" at {dto.BranchName}"
                    : otp.WorkLocation == "Outside" ? " (off-site)" : "";
                return ServiceResponse<AttendanceResponseDTO>.SuccessResult(dto,
                    (status == "Late" ? "Clocked in — marked Late" : "Clocked in — On time") + locSuffix);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying clock-in OTP for employee {EmployeeId}", employeeId);
                return ServiceResponse<AttendanceResponseDTO>.FromException(ex, "Failed to verify clock-in");
            }
        }

        public async Task<ServiceResponse<AttendanceOtpRequestResultDTO>> RequestAdminVerifyAsync(
            int employeeId, double? latitude, double? longitude, string clientIp)
        {
            try
            {
                var employee = await _db.EMEmployees.FirstOrDefaultAsync(e => e.Id == employeeId);
                if (employee == null)
                    return ServiceResponse<AttendanceOtpRequestResultDTO>.FailureResult("No employee record linked to your account");

                var today = DateTime.Today;
                var existing = await _db.EMAttendance
                    .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.AttendanceDate == today);
                if (existing != null && existing.TimeIn.HasValue)
                    return ServiceResponse<AttendanceOtpRequestResultDTO>.FailureResult("You have already clocked in today");

                var closedEvent = await _db.EMWorkEvents
                    .FirstOrDefaultAsync(e => e.Date == today && e.EventType == "Closed");
                if (closedEvent != null)
                    return ServiceResponse<AttendanceOtpRequestResultDTO>.FailureResult(
                        $"Attendance is closed for today ({closedEvent.Name}). Contact HR if this is an error.");

                var loc = await _branchService.ResolveAsync(latitude, longitude);

                if (_branchService.RequireOnSite && !loc.OnSite)
                {
                    var why = loc.WorkLocation == "Unknown"
                        ? "Location is required to clock in. Please enable location access and try again."
                        : "You must be within an office branch to clock in.";
                    return ServiceResponse<AttendanceOtpRequestResultDTO>.FailureResult(why);
                }

                // Retire any prior pending admin-verify requests for this employee.
                var stale = await _db.EMAttendanceOtps
                    .Where(o => o.EmployeeId == employeeId && o.Purpose == "ClockInAdminVerify" && !o.IsUsed)
                    .ToListAsync();
                foreach (var s in stale) s.IsUsed = true;

                var requestedTime = DateTime.Now.TimeOfDay;
                var expiry = DateTime.UtcNow.AddHours(1);

                _db.EMAttendanceOtps.Add(new EMAttendanceOtp
                {
                    EmployeeId = employeeId,
                    Email = employee.Email ?? "",
                    Code = "ADMIN",
                    Purpose = "ClockInAdminVerify",
                    RequestedTime = requestedTime,
                    Latitude = latitude,
                    Longitude = longitude,
                    WorkLocation = loc.WorkLocation,
                    BranchId = loc.BranchId,
                    ExpiresAt = expiry,
                    IsUsed = false,
                    Attempts = 0,
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();

                _logger.LogInformation("Admin-verify clock-in request submitted for employee {EmployeeId} from {Ip}", employeeId, clientIp);

                return ServiceResponse<AttendanceOtpRequestResultDTO>.SuccessResult(new AttendanceOtpRequestResultDTO
                {
                    Sent = true,
                    Message = "Request submitted — waiting for HR/Admin to verify",
                    RequestedTimeFormatted = DateTime.Today.Add(requestedTime).ToString("hh:mm tt"),
                    WorkLocation = loc.WorkLocation,
                    BranchName = loc.BranchName,
                    OnSite = loc.OnSite,
                    DistanceMeters = loc.DistanceMeters
                }, "Verification request submitted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting admin-verify request for employee {EmployeeId}", employeeId);
                return ServiceResponse<AttendanceOtpRequestResultDTO>.FromException(ex, "Failed to submit verification request");
            }
        }

        public async Task<ServiceResponse<List<PendingVerifyRequestDTO>>> GetPendingVerifyRequestsAsync()
        {
            try
            {
                var pending = await _db.EMAttendanceOtps
                    .Where(o => o.Purpose == "ClockInAdminVerify" && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
                    .OrderBy(o => o.CreatedAt)
                    .ToListAsync();

                var employeeIds = pending.Select(p => p.EmployeeId).Distinct().ToList();
                var employees = await _db.EMEmployees
                    .Where(e => employeeIds.Contains(e.Id))
                    .ToListAsync();

                var branchIds = pending.Where(p => p.BranchId.HasValue).Select(p => p.BranchId!.Value).Distinct().ToList();
                var branches = await _db.EMBranches
                    .Where(b => branchIds.Contains(b.Id))
                    .ToDictionaryAsync(b => b.Id, b => b.Name);

                var result = pending.Select(p =>
                {
                    var emp = employees.FirstOrDefault(e => e.Id == p.EmployeeId);
                    return new PendingVerifyRequestDTO
                    {
                        RequestId = p.Id,
                        EmployeeId = p.EmployeeId,
                        EmployeeName = emp != null ? $"{emp.FirstName} {emp.LastName}".Trim() : $"Employee #{p.EmployeeId}",
                        Department = emp?.Department ?? "",
                        RequestedTimeFormatted = DateTime.Today.Add(p.RequestedTime).ToString("hh:mm tt"),
                        WorkLocation = p.WorkLocation ?? "Unknown",
                        BranchName = p.BranchId.HasValue && branches.TryGetValue(p.BranchId.Value, out var bn) ? bn : null,
                        OnSite = p.WorkLocation == "Office",
                        RequestedAt = p.CreatedAt
                    };
                }).ToList();

                return ServiceResponse<List<PendingVerifyRequestDTO>>.SuccessResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending verify requests");
                return ServiceResponse<List<PendingVerifyRequestDTO>>.FromException(ex, "Failed to load pending requests");
            }
        }

        public async Task<ServiceResponse<AttendanceResponseDTO>> ApproveVerifyRequestAsync(
            int requestId, string approverInfo, string clientIp)
        {
            try
            {
                var request = await _db.EMAttendanceOtps
                    .FirstOrDefaultAsync(o => o.Id == requestId && o.Purpose == "ClockInAdminVerify" && !o.IsUsed);

                if (request == null)
                    return ServiceResponse<AttendanceResponseDTO>.FailureResult("Request not found or already processed");

                if (request.ExpiresAt < DateTime.UtcNow)
                {
                    request.IsUsed = true;
                    await _db.SaveChangesAsync();
                    return ServiceResponse<AttendanceResponseDTO>.FailureResult("This request has expired");
                }

                var today = DateTime.Today;
                var existingAttendance = await _db.EMAttendance
                    .FirstOrDefaultAsync(a => a.EmployeeId == request.EmployeeId && a.AttendanceDate == today);
                if (existingAttendance != null && existingAttendance.TimeIn.HasValue)
                {
                    request.IsUsed = true;
                    await _db.SaveChangesAsync();
                    return ServiceResponse<AttendanceResponseDTO>.FailureResult("Employee has already clocked in today");
                }

                request.IsUsed = true;

                var schedule = await _scheduleService.GetEntityAsync(request.EmployeeId);
                var status = _scheduleService.ComputeStatus(schedule, request.RequestedTime);
                var locationNote = request.WorkLocation == "Office" ? "Office"
                    : request.WorkLocation == "Outside" ? "Off-site" : "Unknown location";

                EMAttendance attendance;
                if (existingAttendance != null)
                {
                    existingAttendance.TimeIn = request.RequestedTime;
                    existingAttendance.Status = status;
                    existingAttendance.Shift = DetermineShift(request.RequestedTime);
                    existingAttendance.WorkLocation = request.WorkLocation;
                    existingAttendance.BranchId = request.BranchId;
                    existingAttendance.Latitude = request.Latitude;
                    existingAttendance.Longitude = request.Longitude;
                    existingAttendance.UpdatedAt = DateTime.UtcNow;
                    existingAttendance.Remarks = $"Clock-in (Admin verified, {locationNote}) via {approverInfo ?? "Admin"}";
                    attendance = existingAttendance;
                }
                else
                {
                    attendance = new EMAttendance
                    {
                        EmployeeId = request.EmployeeId,
                        AttendanceDate = today,
                        TimeIn = request.RequestedTime,
                        Status = status,
                        Shift = DetermineShift(request.RequestedTime),
                        WorkLocation = request.WorkLocation,
                        BranchId = request.BranchId,
                        Latitude = request.Latitude,
                        Longitude = request.Longitude,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Remarks = $"Clock-in (Admin verified, {locationNote}) via {approverInfo ?? "Admin"}"
                    };
                    _db.EMAttendance.Add(attendance);
                }

                await _db.SaveChangesAsync();

                attendance.Employee ??= await _db.EMEmployees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId);

                _logger.LogInformation("Admin approved clock-in for employee {EmployeeId} at {Time} → {Status}",
                    request.EmployeeId, request.RequestedTime, status);

                var dto = _mapper.Map<AttendanceResponseDTO>(attendance);
                if (request.BranchId.HasValue)
                    dto.BranchName = (await _db.EMBranches.FirstOrDefaultAsync(b => b.Id == request.BranchId))?.Name;

                var locSuffix = request.WorkLocation == "Office" && dto.BranchName != null ? $" at {dto.BranchName}"
                    : request.WorkLocation == "Outside" ? " (off-site)" : "";
                return ServiceResponse<AttendanceResponseDTO>.SuccessResult(dto,
                    (status == "Late" ? "Clocked in — marked Late" : "Clocked in — On time") + locSuffix);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving verify request {RequestId}", requestId);
                return ServiceResponse<AttendanceResponseDTO>.FromException(ex, "Failed to approve clock-in");
            }
        }

        private static string BuildOtpEmail(string name, string code, int minutes)
        {
            return $@"
<div style=""font-family:Roboto,Arial,sans-serif;max-width:480px;margin:0 auto;color:#202124"">
  <h2 style=""font-weight:500"">Attendance verification</h2>
  <p>Hi {System.Net.WebUtility.HtmlEncode(name)},</p>
  <p>Use this one-time code to confirm your clock-in:</p>
  <div style=""font-size:32px;font-weight:700;letter-spacing:8px;background:#f1f3f4;
              padding:16px;text-align:center;border-radius:8px;margin:16px 0"">{code}</div>
  <p style=""color:#5f6368"">This code expires in {minutes} minutes. If you didn't try to clock in, you can ignore this email.</p>
</div>";
        }

        private static string DetermineShift(TimeSpan timeIn)
        {
            var h = timeIn.Hours;
            if (h < 12) return "Morning";
            if (h < 18) return "Day";
            return "Night";
        }

        private static string MaskEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return "unknown";
            var parts = email.Split('@');
            if (parts.Length != 2) return "invalid-email";
            var name = parts[0];
            if (name.Length <= 2) return $"{name}@{parts[1]}";
            return $"{name[..2]}{new string('*', name.Length - 2)}@{parts[1]}";
        }
    }
}
