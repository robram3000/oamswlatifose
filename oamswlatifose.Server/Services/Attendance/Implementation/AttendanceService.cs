using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Options;
using oamswlatifose.Server.DTO.attendances;
using oamswlatifose.Server.Middleware;
using oamswlatifose.Server.Model.occurance;
using oamswlatifose.Server.Repository.AttendanceManagement.Interfaces;
using oamswlatifose.Server.Repository.AuditManagement.Interfaces;
using oamswlatifose.Server.Repository.EmployeeManagement.Interface;
using oamswlatifose.Server.Services.Attendance.Interfaces;
using System.Text;
using System.Text.Json;

namespace oamswlatifose.Server.Services.Attendance.Implementation
{
    /// <summary>
    /// Comprehensive attendance management service implementing business logic for employee time tracking,
    /// attendance reporting, and administrative management of attendance records.
    /// 
    /// <para>Key Features:</para>
    /// <para>- Automatic hours worked calculation with configurable overtime rules</para>
    /// <para>- Late arrival detection based on shift start times</para>
    /// <para>- Duplicate clock-in prevention</para>
    /// <para>- Geolocation validation for remote work tracking</para>
    /// <para>- Department-wide attendance analytics</para>
    /// <para>- Report generation with multiple formats</para>
    /// <para>- Audit logging for all attendance modifications</para>
    /// 
    /// <para>Business Rules:</para>
    /// <para>- Standard work day: 8 hours (configurable)</para>
    /// <para>- Overtime: hours beyond 8 per day</para>
    /// <para>- Late threshold: 15 minutes after shift start</para>
    /// <para>- Grace period: 5 minutes for clock-in</para>
    /// <para>- Weekend attendance: optional with special handling</para>
    /// </summary>
    public class AttendanceService : BaseService, IAttendanceService
    {
        private readonly IAttendanceTrackingQueryRepository _queryRepository;
        private readonly IAttendanceTrackingCommandRepository _commandRepository;
        private readonly IEmployeeManagementQueryRepository _employeeQueryRepository;
        private readonly IAuthenticationAuditCommandRepository _auditRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateAttendanceDTO> _createValidator;
        private readonly IValidator<UpdateAttendanceDTO> _updateValidator;
        private readonly IValidator<ClockInDTO> _clockInValidator;
        private readonly IValidator<ClockOutDTO> _clockOutValidator;
        private readonly AttendanceSettings _settings;

        /// <summary>
        /// Configuration settings for attendance business rules
        /// </summary>
        public class AttendanceSettings
        {
            public TimeSpan StandardShiftStart { get; set; } = new TimeSpan(9, 0, 0);
            public TimeSpan LateThreshold { get; set; } = new TimeSpan(9, 15, 0);
            public TimeSpan GracePeriod { get; set; } = new TimeSpan(0, 5, 0);
            public decimal StandardHoursPerDay { get; set; } = 8;
            public bool TrackWeekends { get; set; } = false;
            public bool RequireGeolocation { get; set; } = false;
            public double GeofenceRadiusKm { get; set; } = 0.5;
            public (double Lat, double Lon) OfficeLocation { get; set; } = (40.7128, -74.0060); // NYC default
        }

        public AttendanceService(
            IAttendanceTrackingQueryRepository queryRepository,
            IAttendanceTrackingCommandRepository commandRepository,
            IEmployeeManagementQueryRepository employeeQueryRepository,
            IAuthenticationAuditCommandRepository auditRepository,
            IMapper mapper,
            IValidator<CreateAttendanceDTO> createValidator,
            IValidator<UpdateAttendanceDTO> updateValidator,
            IValidator<ClockInDTO> clockInValidator,
            IValidator<ClockOutDTO> clockOutValidator,
            IOptions<AttendanceSettings> settings,
            ILogger<AttendanceService> logger,
            IHttpContextAccessor httpContextAccessor,
            ICorrelationIdGenerator correlationIdGenerator)
            : base(logger, httpContextAccessor, correlationIdGenerator)
        {
            _queryRepository = queryRepository ?? throw new ArgumentNullException(nameof(queryRepository));
            _commandRepository = commandRepository ?? throw new ArgumentNullException(nameof(commandRepository));
            _employeeQueryRepository = employeeQueryRepository ?? throw new ArgumentNullException(nameof(employeeQueryRepository));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
            _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
            _clockInValidator = clockInValidator ?? throw new ArgumentNullException(nameof(clockInValidator));
            _clockOutValidator = clockOutValidator ?? throw new ArgumentNullException(nameof(clockOutValidator));
            _settings = settings?.Value ?? new AttendanceSettings();
        }

        #region Employee Self-Service Operations

        /// <inheritdoc />
        public async Task<ServiceResponse<AttendanceResponseDTO>> ClockInAsync(ClockInDTO clockInDto, string clientIp)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    // Validate input
                    var validationResult = await _clockInValidator.ValidateAsync(clockInDto);
                    if (!validationResult.IsValid)
                    {
                        return ServiceResponse<AttendanceResponseDTO>.FailureResult(
                            "Clock-in validation failed",
                            validationResult.Errors.Select(e => e.ErrorMessage));
                    }

                    // Verify employee exists
                    var employee = await _employeeQueryRepository.GetEmployeeByIdAsync(clockInDto.EmployeeId);
                    if (employee == null)
                    {
                        return ServiceResponse<AttendanceResponseDTO>.FailureResult(
                            $"Employee with ID {clockInDto.EmployeeId} not found");
                    }

                    // Check if already clocked in today
                    var today = DateTime.Today;
                    var existingAttendance = await _queryRepository.GetAttendanceByEmployeeIdAsync(clockInDto.EmployeeId);
                    var todaysAttendance = existingAttendance.FirstOrDefault(a => a.AttendanceDate == today);

                    if (todaysAttendance != null && todaysAttendance.TimeIn.HasValue)
                    {
                        if (!todaysAttendance.TimeOut.HasValue)
                        {
                            return ServiceResponse<AttendanceResponseDTO>.FailureResult(
                                "You are already clocked in today. Please clock out first.");
                        }
                        return ServiceResponse<AttendanceResponseDTO>.FailureResult(
                            "You have already completed attendance for today.");
                    }

                    // Validate geolocation if required
                    if (_settings.RequireGeolocation && clockInDto.Latitude.HasValue && clockInDto.Longitude.HasValue)
                    {
                        var locationValid = IsWithinGeofence(
                            clockInDto.Latitude.Value,
                            clockInDto.Longitude.Value);

                        if (!locationValid)
                        {
                            _logger.LogWarning("Clock-in attempt from outside geofence: Employee {EmployeeId}, Location ({Lat}, {Lon})",
                                clockInDto.EmployeeId, clockInDto.Latitude, clockInDto.Longitude);

                            return ServiceResponse<AttendanceResponseDTO>.FailureResult(
                                "You must be within the office geofence to clock in.");
                        }
                    }

                    // Determine status based on time
                    var timeIn = clockInDto.TimeIn ?? DateTime.Now.TimeOfDay;
                    var status = DetermineClockInStatus(timeIn);

                    // Create attendance record
                    var attendance = new EMAttendance
                    {
                        EmployeeId = clockInDto.EmployeeId,
                        AttendanceDate = today,
                        TimeIn = timeIn,
                        Status = status,
                        Shift = DetermineShift(timeIn),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Remarks = $"Clock-in via {clockInDto.DeviceInfo ?? "Unknown device"}"
                    };

                    var created = await _commandRepository.CreateAttendanceAsync(attendance);

                    // Log audit
                    await _auditRepository.LogSuccessfulAuthenticationAsync(
                        CurrentUserId ?? 0,
                        CurrentUsername ?? "system",
                        "ClockIn",
                        clientIp,
                        UserAgent,
                        "Web",
                        null,
                        $"Employee {clockInDto.EmployeeId} clocked in at {timeIn}");

                    _logger.LogInformation("Employee {EmployeeId} clocked in at {TimeIn} with status {Status}",
                        clockInDto.EmployeeId, timeIn, status);

                    var result = _mapper.Map<AttendanceResponseDTO>(created);
                    return ServiceResponse<AttendanceResponseDTO>.SuccessResult(result, "Clock-in successful");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during clock-in for employee {EmployeeId}", clockInDto?.EmployeeId);
                    return ServiceResponse<AttendanceResponseDTO>.FromException(ex, "Clock-in failed");
                }
            }, "ClockInAsync");
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<AttendanceResponseDTO>> LogTimeOffAsync(int employeeId)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    var today = DateTime.Today;
                    var existing = (await _queryRepository.GetAttendanceByEmployeeIdAsync(employeeId))
                        .FirstOrDefault(a => a.AttendanceDate == today);

                    if (existing != null)
                        return ServiceResponse<AttendanceResponseDTO>.FailureResult(
                            "You already have an attendance record for today.");

                    var record = new EMAttendance
                    {
                        EmployeeId = employeeId,
                        AttendanceDate = today,
                        Status = "Time Off",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Remarks = "Time off"
                    };

                    var created = await _commandRepository.CreateAttendanceAsync(record);
                    _logger.LogInformation("Time off logged for employee {EmployeeId}", employeeId);
                    return ServiceResponse<AttendanceResponseDTO>.SuccessResult(
                        _mapper.Map<AttendanceResponseDTO>(created), "Time off recorded for today.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error logging time off for employee {EmployeeId}", employeeId);
                    return ServiceResponse<AttendanceResponseDTO>.FromException(ex, "Failed to log time off");
                }
            }, "LogTimeOffAsync");
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<AttendanceResponseDTO>> ClockOutAsync(ClockOutDTO clockOutDto, string clientIp)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    // Validate input
                    var validationResult = await _clockOutValidator.ValidateAsync(clockOutDto);
                    if (!validationResult.IsValid)
                    {
                        return ServiceResponse<AttendanceResponseDTO>.FailureResult(
                            "Clock-out validation failed",
                            validationResult.Errors.Select(e => e.ErrorMessage));
                    }

                    // Find today's attendance record
                    var today = DateTime.Today;
                    var existingAttendance = await _queryRepository.GetAttendanceByEmployeeIdAsync(clockOutDto.EmployeeId);
                    var todaysAttendance = existingAttendance.FirstOrDefault(a => a.AttendanceDate == today);

                    if (todaysAttendance == null)
                    {
                        return ServiceResponse<AttendanceResponseDTO>.FailureResult(
                            "No clock-in record found for today. Please clock in first.");
                    }

                    if (todaysAttendance.TimeOut.HasValue)
                    {
                        return ServiceResponse<AttendanceResponseDTO>.FailureResult(
                            "You have already clocked out today.");
                    }

                    // Update with clock-out time
                    var timeOut = clockOutDto.TimeOut ?? DateTime.Now.TimeOfDay;
                    todaysAttendance.TimeOut = timeOut;

                    // Calculate hours worked
                    CalculateHoursWorked(todaysAttendance);

                    // Update status based on hours
                    todaysAttendance.Status = DetermineClockOutStatus(todaysAttendance);
                    todaysAttendance.UpdatedAt = DateTime.UtcNow;

                    var updated = await _commandRepository.UpdateAttendanceAsync(todaysAttendance);

                    // Log audit
                    await _auditRepository.LogSuccessfulAuthenticationAsync(
                        CurrentUserId ?? 0,
                        CurrentUsername ?? "system",
                        "ClockOut",
                        clientIp,
                        UserAgent,
                        "Web",
                        null,
                        $"Employee {clockOutDto.EmployeeId} clocked out at {timeOut}, hours worked: {todaysAttendance.HoursWorked}");

                    _logger.LogInformation("Employee {EmployeeId} clocked out at {TimeOut}, hours worked: {HoursWorked}",
                        clockOutDto.EmployeeId, timeOut, todaysAttendance.HoursWorked);

                    var result = _mapper.Map<AttendanceResponseDTO>(updated);
                    return ServiceResponse<AttendanceResponseDTO>.SuccessResult(result, "Clock-out successful");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during clock-out for employee {EmployeeId}", clockOutDto?.EmployeeId);
                    return ServiceResponse<AttendanceResponseDTO>.FromException(ex, "Clock-out failed");
                }
            }, "ClockOutAsync");
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<AttendanceResponseDTO>> GetTodayAttendanceAsync(int employeeId)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    var today = DateTime.Today;
                    var attendance = await _queryRepository.GetAttendanceByEmployeeIdAsync(employeeId);
                    var todaysRecord = attendance.FirstOrDefault(a => a.AttendanceDate == today);

                    if (todaysRecord == null)
                    {
                        return ServiceResponse<AttendanceResponseDTO>.SuccessResult(
                            null, "No attendance record for today");
                    }

                    var result = _mapper.Map<AttendanceResponseDTO>(todaysRecord);
                    return ServiceResponse<AttendanceResponseDTO>.SuccessResult(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting today's attendance for employee {EmployeeId}", employeeId);
                    return ServiceResponse<AttendanceResponseDTO>.FromException(ex, "Failed to get today's attendance");
                }
            }, "GetTodayAttendanceAsync");
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<PagedResult<AttendanceSummaryDTO>>> GetEmployeeAttendanceHistoryAsync(
            int employeeId, int pageNumber, int pageSize)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    var attendance = await _queryRepository.GetEmployeeAttendancePaginatedAsync(
                        employeeId, pageNumber, pageSize);

                    var totalCount = (await _queryRepository.GetAttendanceByEmployeeIdAsync(employeeId)).Count();

                    var dtos = _mapper.Map<IEnumerable<AttendanceSummaryDTO>>(attendance);

                    var result = new PagedResult<AttendanceSummaryDTO>
                    {
                        Items = dtos,
                        TotalCount = totalCount,
                        PageNumber = pageNumber,
                        PageSize = pageSize
                    };

                    return ServiceResponse<PagedResult<AttendanceSummaryDTO>>.SuccessResult(
                        result, $"Retrieved {dtos.Count()} of {totalCount} records");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting attendance history for employee {EmployeeId}", employeeId);
                    return ServiceResponse<PagedResult<AttendanceSummaryDTO>>.FromException(
                        ex, "Failed to get attendance history");
                }
            }, "GetEmployeeAttendanceHistoryAsync");
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<EmployeeAttendanceSummaryDTO>> GetEmployeeAttendanceSummaryAsync(
            int employeeId, DateTime startDate, DateTime endDate)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    var attendance = await _queryRepository.GetEmployeeAttendanceByDateRangeAsync(
                        employeeId, startDate, endDate);

                    var totalDays = (endDate - startDate).Days + 1;
                    var workingDays = totalDays; // Subtract weekends/holidays as needed

                    var summary = new EmployeeAttendanceSummaryDTO
                    {
                        EmployeeId = employeeId,
                        DaysPresent = attendance.Count(a => a.Status == "Present" || a.Status == "Late"),
                        DaysAbsent = workingDays - attendance.Count(a => a.Status == "Present" || a.Status == "Late"),
                        DaysLate = attendance.Count(a => a.Status == "Late"),
                        TotalHoursWorked = attendance.Sum(a => a.HoursWorked ?? 0),
                        TotalOvertimeHours = attendance.Sum(a => a.OvertimeHours ?? 0),
                        AttendancePercentage = workingDays > 0
                            ? Math.Round((double)attendance.Count(a => a.Status == "Present" || a.Status == "Late") / workingDays * 100, 2)
                            : 0
                    };

                    // Get employee details
                    var employee = await _employeeQueryRepository.GetEmployeeByIdAsync(employeeId);
                    if (employee != null)
                    {
                        summary.EmployeeName = $"{employee.FirstName} {employee.LastName}";
                        summary.Department = employee.Department;
                    }

                    return ServiceResponse<EmployeeAttendanceSummaryDTO>.SuccessResult(summary);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting attendance summary for employee {EmployeeId}", employeeId);
                    return ServiceResponse<EmployeeAttendanceSummaryDTO>.FromException(
                        ex, "Failed to get attendance summary");
                }
            }, "GetEmployeeAttendanceSummaryAsync");
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<IEnumerable<AttendanceResponseDTO>>> GetEmployeeAttendanceByDateRangeAsync(
            int employeeId, DateTime startDate, DateTime endDate)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    var attendance = await _queryRepository.GetEmployeeAttendanceByDateRangeAsync(
                        employeeId, startDate, endDate);

                    var result = _mapper.Map<IEnumerable<AttendanceResponseDTO>>(attendance);

                    return ServiceResponse<IEnumerable<AttendanceResponseDTO>>.SuccessResult(
                        result, $"Retrieved {result.Count()} records");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting attendance by date range for employee {EmployeeId}", employeeId);
                    return ServiceResponse<IEnumerable<AttendanceResponseDTO>>.FromException(
                        ex, "Failed to get attendance records");
                }
            }, "GetEmployeeAttendanceByDateRangeAsync");
        }

        #endregion

        #region Manager/Admin Query Operations

        /// <inheritdoc />
        public async Task<ServiceResponse<PagedResult<AttendanceResponseDTO>>> GetAllAttendanceAsync(AttendanceFilterDTO filter)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    IEnumerable<EMAttendance> attendance;

                    if (filter.StartDate.HasValue && filter.EndDate.HasValue)
                    {
                        attendance = await _queryRepository.GetAttendanceByDateRangeAsync(
                            filter.StartDate.Value, filter.EndDate.Value);
                    }
                    else if (!string.IsNullOrEmpty(filter.Department))
                    {
                        // Get all employees in department then their attendance
                        var employees = await _employeeQueryRepository.GetEmployeesByDepartmentAsync(filter.Department);
                        var employeeIds = employees.Select(e => e.Id).ToList();

                        var allAttendance = new List<EMAttendance>();
                        foreach (var empId in employeeIds)
                        {
                            var empAttendance = await _queryRepository.GetAttendanceByEmployeeIdAsync(empId);
                            allAttendance.AddRange(empAttendance);
                        }

                        attendance = allAttendance
                            .OrderByDescending(a => a.AttendanceDate)
                            .Skip((filter.PageNumber - 1) * filter.PageSize)
                            .Take(filter.PageSize);
                    }
                    else
                    {
                        attendance = await _queryRepository.GetAllAttendanceRecordsAsync();
                    }

                    // Apply additional filters
                    if (!string.IsNullOrEmpty(filter.Status))
                    {
                        attendance = attendance.Where(a => a.Status == filter.Status);
                    }

                    if (!string.IsNullOrEmpty(filter.Shift))
                    {
                        attendance = attendance.Where(a => a.Shift == filter.Shift);
                    }

                    var totalCount = attendance.Count();
                    var pagedAttendance = attendance
                        .OrderByDescending(a => a.AttendanceDate)
                        .Skip((filter.PageNumber - 1) * filter.PageSize)
                        .Take(filter.PageSize)
                        .ToList();

                    var dtos = _mapper.Map<IEnumerable<AttendanceResponseDTO>>(pagedAttendance);

                    var result = new PagedResult<AttendanceResponseDTO>
                    {
                        Items = dtos,
                        TotalCount = totalCount,
                        PageNumber = filter.PageNumber,
                        PageSize = filter.PageSize
                    };

                    return ServiceResponse<PagedResult<AttendanceResponseDTO>>.SuccessResult(
                        result, $"Retrieved {dtos.Count()} of {totalCount} records");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting all attendance records with filters");
                    return ServiceResponse<PagedResult<AttendanceResponseDTO>>.FromException(
                        ex, "Failed to get attendance records");
                }
            }, "GetAllAttendanceAsync");
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<AttendanceResponseDTO>> GetAttendanceByIdAsync(int id)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    var attendance = await _queryRepository.GetAttendanceByIdAsync(id);

                    if (attendance == null)
                    {
                        return ServiceResponse<AttendanceResponseDTO>.FailureResult(
                            $"Attendance record with ID {id} not found");
                    }

                    var result = _mapper.Map<AttendanceResponseDTO>(attendance);
                    return ServiceResponse<AttendanceResponseDTO>.SuccessResult(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting attendance record {Id}", id);
                    return ServiceResponse<AttendanceResponseDTO>.FromException(
                        ex, "Failed to get attendance record");
                }
            }, "GetAttendanceByIdAsync");
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<IEnumerable<AttendanceResponseDTO>>> GetAttendanceByDateAsync(DateTime date)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    var attendance = await _queryRepository.GetAttendanceByDateAsync(date);
                    var result = _mapper.Map<IEnumerable<AttendanceResponseDTO>>(attendance);

                    return ServiceResponse<IEnumerable<AttendanceResponseDTO>>.SuccessResult(
                        result, $"Found {result.Count()} records for {date:yyyy-MM-dd}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting attendance for date {Date}", date);
                    return ServiceResponse<IEnumerable<AttendanceResponseDTO>>.FromException(
                        ex, "Failed to get attendance records");
                }
            }, "GetAttendanceByDateAsync");
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<IEnumerable<AttendanceResponseDTO>>> GetAttendanceByDateRangeAsync(
            DateTime startDate, DateTime endDate, string department = null)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    IEnumerable<EMAttendance> attendance;

                    if (!string.IsNullOrEmpty(department))
                    {
                        // Get employees in department
                        var employees = await _employeeQueryRepository.GetEmployeesByDepartmentAsync(department);
                        var employeeIds = employees.Select(e => e.Id).ToList();

                        var allAttendance = new List<EMAttendance>();
                        foreach (var empId in employeeIds)
                        {
                            var empAttendance = await _queryRepository.GetEmployeeAttendanceByDateRangeAsync(
                                empId, startDate, endDate);
                            allAttendance.AddRange(empAttendance);
                        }

                        attendance = allAttendance.OrderBy(a => a.AttendanceDate);
                    }
                    else
                    {
                        attendance = await _queryRepository.GetAttendanceByDateRangeAsync(startDate, endDate);
                    }

                    var result = _mapper.Map<IEnumerable<AttendanceResponseDTO>>(attendance);

                    return ServiceResponse<IEnumerable<AttendanceResponseDTO>>.SuccessResult(
                        result, $"Found {result.Count()} records");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting attendance by date range");
                    return ServiceResponse<IEnumerable<AttendanceResponseDTO>>.FromException(
                        ex, "Failed to get attendance records");
                }
            }, "GetAttendanceByDateRangeAsync");
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<IEnumerable<AbsentEmployeeDTO>>> GetAbsentEmployeesAsync(DateTime date)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    var absentEmployees = await _queryRepository.GetAbsentEmployeesByDateAsync(date);

                    var result = absentEmployees.Select(e => new AbsentEmployeeDTO
                    {
                        EmployeeId = e.Id,
                        EmployeeName = $"{e.FirstName} {e.LastName}",
                        Department = e.Department,
                        Position = e.Position,
                        HasApprovedLeave = false // TODO: Check leave system
                    });

                    return ServiceResponse<IEnumerable<AbsentEmployeeDTO>>.SuccessResult(
                        result, $"Found {result.Count()} absent employees");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting absent employees for date {Date}", date);
                    return ServiceResponse<IEnumerable<AbsentEmployeeDTO>>.FromException(
                        ex, "Failed to get absent employees");
                }
            }, "GetAbsentEmployeesAsync");
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<IEnumerable<LateArrivalDTO>>> GetLateArrivalsAsync(
            DateTime date, TimeSpan lateThreshold)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    var lateAttendances = await _queryRepository.GetLateArrivalsByDateAsync(date, lateThreshold);

                    var result = lateAttendances.Select(a => new LateArrivalDTO
                    {
                        EmployeeId = a.EmployeeId,
                        EmployeeName = a.Employee != null ? $"{a.Employee.FirstName} {a.Employee.LastName}" : "Unknown",
                        Department = a.Employee?.Department,
                        Date = a.AttendanceDate,
                        TimeIn = a.TimeIn.Value,
                        ExpectedTime = _settings.StandardShiftStart,
                        MinutesLate = (int)(a.TimeIn.Value - _settings.StandardShiftStart).TotalMinutes
                    });

                    return ServiceResponse<IEnumerable<LateArrivalDTO>>.SuccessResult(
                        result, $"Found {result.Count()} late arrivals");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting late arrivals for date {Date}", date);
                    return ServiceResponse<IEnumerable<LateArrivalDTO>>.FromException(
                        ex, "Failed to get late arrivals");
                }
            }, "GetLateArrivalsAsync");
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<DepartmentAttendanceStatsDTO>> GetDepartmentStatisticsAsync(
            string department, DateTime startDate, DateTime endDate)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    // Get all employees in department
                    var employees = await _employeeQueryRepository.GetEmployeesByDepartmentAsync(department);
                    var employeeIds = employees.Select(e => e.Id).ToList();

                    // Get attendance for all employees in date range
                    var allAttendance = new List<EMAttendance>();
                    foreach (var empId in employeeIds)
                    {
                        var empAttendance = await _queryRepository.GetEmployeeAttendanceByDateRangeAsync(
                            empId, startDate, endDate);
                        allAttendance.AddRange(empAttendance);
                    }

                    var workingDays = (endDate - startDate).Days + 1;

                    var statusBreakdown = new Dictionary<string, int>
                    {
                        ["Present"] = allAttendance.Count(a => a.Status == "Present"),
                        ["Late"] = allAttendance.Count(a => a.Status == "Late"),
                        ["Absent"] = (employeeIds.Count * workingDays) - allAttendance.Count
                    };

                    // Calculate total hours as decimal explicitly
                    decimal totalHours = allAttendance.Sum(a => a.HoursWorked ?? 0);

                    // Calculate average with explicit decimal conversion
                    decimal averageHoursPerDay;
                    if (workingDays > 0)
                    {
                        averageHoursPerDay = Math.Round(totalHours / (decimal)workingDays, 2);
                    }
                    else
                    {
                        averageHoursPerDay = 0m;
                    }

                    // Calculate attendance rate - FIXED: Cast to decimal
                    decimal attendanceRate = 0m;
                    if (employeeIds.Count > 0 && workingDays > 0)
                    {
                        double rate = (double)statusBreakdown["Present"] / (employeeIds.Count * workingDays) * 100;
                        attendanceRate = (decimal)Math.Round(rate, 2);
                    }

                    var topPerformers = allAttendance
                        .GroupBy(a => a.EmployeeId)
                        .Select(g => new EmployeeAttendanceSummaryDTO
                        {
                            EmployeeId = g.Key,
                            EmployeeName = g.First().Employee != null
                                ? $"{g.First().Employee.FirstName} {g.First().Employee.LastName}"
                                : "Unknown",
                            DaysPresent = g.Count(a => a.Status == "Present" || a.Status == "Late"),
                            TotalHoursWorked = g.Sum(a => a.HoursWorked ?? 0)
                        })
                        .OrderByDescending(x => x.DaysPresent)
                        .Take(5)
                        .ToList();

                    var stats = new DepartmentAttendanceStatsDTO
                    {
                        Department = department,
                        TotalEmployees = employeeIds.Count,
                        StartDate = startDate,
                        EndDate = endDate,
                        TotalWorkingDays = workingDays,
                        AttendanceRate = attendanceRate, 
                        AverageHoursPerDay = averageHoursPerDay,  // Now decimal
                        StatusBreakdown = statusBreakdown,
                        TopPerformers = topPerformers
                    };

                    return ServiceResponse<DepartmentAttendanceStatsDTO>.SuccessResult(stats);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting department statistics for {Department}", department);
                    return ServiceResponse<DepartmentAttendanceStatsDTO>.FromException(
                        ex, "Failed to get department statistics");
                }
            }, "GetDepartmentStatisticsAsync");
        }
        #endregion

        #region Report Generation

        /// <inheritdoc />
        public async Task<ServiceResponse<AttendanceReportDataDTO>> GenerateAttendanceReportAsync(AttendanceReportRequestDTO request)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    IEnumerable<EMAttendance> attendance;

                    if (request.EmployeeId.HasValue)
                    {
                        attendance = await _queryRepository.GetEmployeeAttendanceByDateRangeAsync(
                            request.EmployeeId.Value, request.StartDate, request.EndDate);
                    }
                    else if (!string.IsNullOrEmpty(request.Department))
                    {
                        var employees = await _employeeQueryRepository.GetEmployeesByDepartmentAsync(request.Department);
                        var employeeIds = employees.Select(e => e.Id).ToList();

                        var allAttendance = new List<EMAttendance>();
                        foreach (var empId in employeeIds)
                        {
                            var empAttendance = await _queryRepository.GetEmployeeAttendanceByDateRangeAsync(
                                empId, request.StartDate, request.EndDate);
                            allAttendance.AddRange(empAttendance);
                        }

                        attendance = allAttendance;
                    }
                    else
                    {
                        attendance = await _queryRepository.GetAttendanceByDateRangeAsync(
                            request.StartDate, request.EndDate);
                    }

                    var attendanceList = attendance.ToList();
                    var totalEmployees = attendanceList.Select(a => a.EmployeeId).Distinct().Count();
                    var totalDays = (request.EndDate - request.StartDate).Days + 1;

                    var report = new AttendanceReportDataDTO
                    {
                        GeneratedAt = DateTime.UtcNow,
                        StartDate = request.StartDate,
                        EndDate = request.EndDate,
                        TotalEmployees = totalEmployees,
                        TotalDays = totalDays,
                        TotalPresent = attendanceList.Count(a => a.Status == "Present"),
                        TotalAbsent = attendanceList.Count(a => a.Status == "Absent"),
                        TotalLate = attendanceList.Count(a => a.Status == "Late"),
                        TotalHoursWorked = attendanceList.Sum(a => a.HoursWorked ?? 0),
                        TotalOvertimeHours = attendanceList.Sum(a => a.OvertimeHours ?? 0),
                        EmployeeSummaries = attendanceList
                            .GroupBy(a => a.EmployeeId)
                            .Select(g => new EmployeeAttendanceSummaryDTO
                            {
                                EmployeeId = g.Key,
                                EmployeeName = g.First().Employee != null
                                    ? $"{g.First().Employee.FirstName} {g.First().Employee.LastName}"
                                    : "Unknown",
                                Department = g.First().Employee?.Department,
                                DaysPresent = g.Count(a => a.Status == "Present" || a.Status == "Late"),
                                DaysAbsent = totalDays - g.Count(),
                                DaysLate = g.Count(a => a.Status == "Late"),
                                TotalHoursWorked = g.Sum(a => a.HoursWorked ?? 0),
                                TotalOvertimeHours = g.Sum(a => a.OvertimeHours ?? 0),
                                AttendancePercentage = totalDays > 0
                                    ? Math.Round((double)g.Count() / totalDays * 100, 2)
                                    : 0
                            })
                            .ToList()
                    };

                    return ServiceResponse<AttendanceReportDataDTO>.SuccessResult(report, "Report generated successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating attendance report");
                    return ServiceResponse<AttendanceReportDataDTO>.FromException(ex, "Failed to generate report");
                }
            }, "GenerateAttendanceReportAsync");
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<byte[]>> ExportAttendanceAsync(AttendanceExportRequestDTO request)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    // Get attendance data
                    var attendance = await _queryRepository.GetAttendanceByDateRangeAsync(
                        request.StartDate, request.EndDate);

                    if (!string.IsNullOrEmpty(request.Department))
                    {
                        var employees = await _employeeQueryRepository.GetEmployeesByDepartmentAsync(request.Department);
                        var employeeIds = employees.Select(e => e.Id).ToHashSet();
                        attendance = attendance.Where(a => employeeIds.Contains(a.EmployeeId));
                    }

                    var attendanceList = attendance.ToList();

                    byte[] exportData = request.Format.ToLower() switch
                    {
                        "csv" => GenerateCsvExport(attendanceList),
                        "excel" => await GenerateExcelExport(attendanceList, request),
                        "pdf" => await GeneratePdfExport(attendanceList, request),
                        _ => GenerateJsonExport(attendanceList)
                    };

                    return ServiceResponse<byte[]>.SuccessResult(
                        exportData,
                        $"Export generated successfully in {request.Format} format");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error exporting attendance data to {Format}", request.Format);
                    return ServiceResponse<byte[]>.FromException(ex, "Failed to export attendance data");
                }
            }, "ExportAttendanceAsync");
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<AttendanceDashboardDTO>> GetAttendanceDashboardAsync()
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    var today = DateTime.Today;
                    var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
                    var startOfMonth = new DateTime(today.Year, today.Month, 1);

                    // Today's stats
                    var todayAttendance = await _queryRepository.GetAttendanceByDateAsync(today);
                    var totalEmployees = await _employeeQueryRepository.GetTotalEmployeeCountAsync();

                    // Week stats
                    var weekAttendance = await _queryRepository.GetAttendanceByDateRangeAsync(
                        startOfWeek, today);

                    // Month stats
                    var monthAttendance = await _queryRepository.GetAttendanceByDateRangeAsync(
                        startOfMonth, today);

                    // Calculate dashboard data
                    var dashboard = new AttendanceDashboardDTO
                    {
                        Date = today,
                        TotalEmployees = totalEmployees,

                        TodayStats = new DailyAttendanceStatsDTO
                        {
                            Present = todayAttendance.Count(a => a.Status == "Present"),
                            Late = todayAttendance.Count(a => a.Status == "Late"),
                            Absent = totalEmployees - todayAttendance.Count(),
                            NotClockedIn = totalEmployees - todayAttendance.Count(a => a.TimeIn.HasValue),
                            ClockedIn = todayAttendance.Count(a => a.TimeIn.HasValue && !a.TimeOut.HasValue),
                            Completed = todayAttendance.Count(a => a.TimeIn.HasValue && a.TimeOut.HasValue)
                        },

                        WeeklyStats = new PeriodAttendanceStatsDTO
                        {
                            StartDate = startOfWeek,
                            EndDate = today,
                            TotalDays = (today - startOfWeek).Days + 1,
                            AverageDailyAttendance = Math.Round(
                                weekAttendance.GroupBy(a => a.AttendanceDate).Average(g => g.Count()), 2),
                            TotalHoursWorked = weekAttendance.Sum(a => a.HoursWorked ?? 0),
                            TotalOvertimeHours = weekAttendance.Sum(a => a.OvertimeHours ?? 0)
                        },

                        MonthlyStats = new PeriodAttendanceStatsDTO
                        {
                            StartDate = startOfMonth,
                            EndDate = today,
                            TotalDays = (today - startOfMonth).Days + 1,
                            AverageDailyAttendance = Math.Round(
                                monthAttendance.GroupBy(a => a.AttendanceDate).Average(g => g.Count()), 2),
                            TotalHoursWorked = monthAttendance.Sum(a => a.HoursWorked ?? 0),
                            TotalOvertimeHours = monthAttendance.Sum(a => a.OvertimeHours ?? 0)
                        },

                        RecentActivity = todayAttendance
                            .OrderByDescending(a => a.TimeIn)
                            .Take(10)
                            .Select(a => new AttendanceActivityDTO
                            {
                                EmployeeId = a.EmployeeId,
                                EmployeeName = a.Employee != null
                                    ? $"{a.Employee.FirstName} {a.Employee.LastName}"
                                    : "Unknown",
                                TimeIn = a.TimeIn,
                                TimeOut = a.TimeOut,
                                Status = a.Status
                            })
                            .ToList(),

                        Alerts = new List<AttendanceAlertDTO>()
                    };

                    // Add alerts for anomalies
                    if (dashboard.TodayStats.Absent > totalEmployees * 0.2) // 20% absent
                    {
                        dashboard.Alerts.Add(new AttendanceAlertDTO
                        {
                            Type = "Warning",
                            Message = $"High absence rate today: {dashboard.TodayStats.Absent} employees absent",
                            Severity = "Medium"
                        });
                    }

                    if (dashboard.TodayStats.Late > totalEmployees * 0.15) // 15% late
                    {
                        dashboard.Alerts.Add(new AttendanceAlertDTO
                        {
                            Type = "Warning",
                            Message = $"High late arrivals today: {dashboard.TodayStats.Late} employees late",
                            Severity = "Low"
                        });
                    }

                    return ServiceResponse<AttendanceDashboardDTO>.SuccessResult(dashboard);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating attendance dashboard");
                    return ServiceResponse<AttendanceDashboardDTO>.FromException(ex, "Failed to generate dashboard");
                }
            }, "GetAttendanceDashboardAsync");
        }

        #endregion

        #region Admin CRUD Operations

        /// <inheritdoc />
        public async Task<ServiceResponse<AttendanceResponseDTO>> CreateAttendanceAsync(
            CreateAttendanceDTO createDto, int createdByUserId)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    // Validate
                    var validationResult = await _createValidator.ValidateAsync(createDto);
                    if (!validationResult.IsValid)
                    {
                        return ServiceResponse<AttendanceResponseDTO>.FailureResult(
                            "Validation failed",
                            validationResult.Errors.Select(e => e.ErrorMessage));
                    }

                    // Check for duplicate
                    var existing = await _queryRepository.GetAttendanceByEmployeeIdAsync(createDto.EmployeeId);
                    if (existing.Any(a => a.AttendanceDate == createDto.AttendanceDate))
                    {
                        return ServiceResponse<AttendanceResponseDTO>.FailureResult(
                            $"Attendance record already exists for employee {createDto.EmployeeId} on {createDto.AttendanceDate:d}");
                    }

                    // Create record
                    var attendance = _mapper.Map<EMAttendance>(createDto);

                    if (attendance.TimeIn.HasValue && attendance.TimeOut.HasValue)
                    {
                        CalculateHoursWorked(attendance);
                    }

                    attendance.CreatedAt = DateTime.UtcNow;
                    attendance.UpdatedAt = DateTime.UtcNow;

                    var created = await _commandRepository.CreateAttendanceAsync(attendance);

                    _logger.LogInformation("Admin {AdminId} created attendance record for employee {EmployeeId} on {Date}",
                        createdByUserId, createDto.EmployeeId, createDto.AttendanceDate);

                    var result = _mapper.Map<AttendanceResponseDTO>(created);
                    return ServiceResponse<AttendanceResponseDTO>.SuccessResult(result, "Attendance record created");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating attendance record");
                    return ServiceResponse<AttendanceResponseDTO>.FromException(ex, "Failed to create attendance record");
                }
            }, "CreateAttendanceAsync");
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<AttendanceResponseDTO>> UpdateAttendanceAsync(
            int id, UpdateAttendanceDTO updateDto, int updatedByUserId)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    // Validate
                    var validationResult = await _updateValidator.ValidateAsync(updateDto);
                    if (!validationResult.IsValid)
                    {
                        return ServiceResponse<AttendanceResponseDTO>.FailureResult(
                            "Validation failed",
                            validationResult.Errors.Select(e => e.ErrorMessage));
                    }

                    // Get existing
                    var existing = await _queryRepository.GetAttendanceByIdAsync(id);
                    if (existing == null)
                    {
                        return ServiceResponse<AttendanceResponseDTO>.FailureResult(
                            $"Attendance record with ID {id} not found");
                    }

                    // Apply updates
                    _mapper.Map(updateDto, existing);

                    if (existing.TimeIn.HasValue && existing.TimeOut.HasValue)
                    {
                        CalculateHoursWorked(existing);
                    }

                    existing.UpdatedAt = DateTime.UtcNow;

                    var updated = await _commandRepository.UpdateAttendanceAsync(existing);

                    _logger.LogInformation("Admin {AdminId} updated attendance record {Id}", updatedByUserId, id);

                    var result = _mapper.Map<AttendanceResponseDTO>(updated);
                    return ServiceResponse<AttendanceResponseDTO>.SuccessResult(result, "Attendance record updated");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating attendance record {Id}", id);
                    return ServiceResponse<AttendanceResponseDTO>.FromException(ex, "Failed to update attendance record");
                }
            }, "UpdateAttendanceAsync");
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<bool>> DeleteAttendanceAsync(int id, int deletedByUserId)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    var result = await _commandRepository.DeleteAttendanceAsync(id);

                    if (result)
                    {
                        _logger.LogWarning("Admin {AdminId} deleted attendance record {Id}", deletedByUserId, id);
                        return ServiceResponse<bool>.SuccessResult(true, "Attendance record deleted");
                    }

                    return ServiceResponse<bool>.FailureResult($"Failed to delete attendance record with ID {id}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting attendance record {Id}", id);
                    return ServiceResponse<bool>.FromException(ex, "Failed to delete attendance record");
                }
            }, "DeleteAttendanceAsync");
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<BulkImportResultDTO>> BulkImportAttendanceAsync(
            List<CreateAttendanceDTO> records, int importedByUserId)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                var result = new BulkImportResultDTO
                {
                    TotalRecords = records.Count,
                    SuccessCount = 0,
                    FailCount = 0,
                    Errors = new List<string>(),
                    CreatedIds = new List<int>()
                };

                var attendanceToCreate = new List<EMAttendance>();

                foreach (var record in records)
                {
                    try
                    {
                        // Validate
                        var validationResult = await _createValidator.ValidateAsync(record);
                        if (!validationResult.IsValid)
                        {
                            result.FailCount++;
                            result.Errors.Add($"Record for employee {record.EmployeeId} on {record.AttendanceDate:d}: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
                            continue;
                        }

                        // Check for duplicate
                        var existing = await _queryRepository.GetAttendanceByEmployeeIdAsync(record.EmployeeId);
                        if (existing.Any(a => a.AttendanceDate == record.AttendanceDate))
                        {
                            result.FailCount++;
                            result.Errors.Add($"Duplicate record for employee {record.EmployeeId} on {record.AttendanceDate:d}");
                            continue;
                        }

                        var attendance = _mapper.Map<EMAttendance>(record);

                        if (attendance.TimeIn.HasValue && attendance.TimeOut.HasValue)
                        {
                            CalculateHoursWorked(attendance);
                        }

                        attendance.CreatedAt = DateTime.UtcNow;
                        attendance.UpdatedAt = DateTime.UtcNow;

                        attendanceToCreate.Add(attendance);
                        result.SuccessCount++;
                    }
                    catch (Exception ex)
                    {
                        result.FailCount++;
                        result.Errors.Add($"Error processing record: {ex.Message}");
                    }
                }

                if (attendanceToCreate.Any())
                {
                    var created = await _commandRepository.BulkCreateAttendanceAsync(attendanceToCreate);
                    result.CreatedIds = attendanceToCreate.Select(a => a.Id).ToList();
                }

                _logger.LogInformation("Bulk import completed by admin {AdminId}: {SuccessCount} successful, {FailCount} failed",
                    importedByUserId, result.SuccessCount, result.FailCount);

                return ServiceResponse<BulkImportResultDTO>.SuccessResult(result, "Bulk import completed");
            }, "BulkImportAttendanceAsync");
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<AttendanceResponseDTO>> ApproveAttendanceAsync(
            int id, bool isApproved, string comments, int approvedByUserId)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    var attendance = await _queryRepository.GetAttendanceByIdAsync(id);
                    if (attendance == null)
                    {
                        return ServiceResponse<AttendanceResponseDTO>.FailureResult(
                            $"Attendance record with ID {id} not found");
                    }

                    // Add approval notes to remarks
                    var approvalNote = $"Approved: {isApproved} by user {approvedByUserId} on {DateTime.Now:yyyy-MM-dd HH:mm}. Comments: {comments}";
                    attendance.Remarks = string.IsNullOrEmpty(attendance.Remarks)
                        ? approvalNote
                        : $"{attendance.Remarks}\n{approvalNote}";

                    attendance.UpdatedAt = DateTime.UtcNow;

                    var updated = await _commandRepository.UpdateAttendanceAsync(attendance);

                    _logger.LogInformation("Admin {AdminId} {Action} attendance record {Id}",
                        approvedByUserId, isApproved ? "approved" : "rejected", id);

                    var result = _mapper.Map<AttendanceResponseDTO>(updated);
                    return ServiceResponse<AttendanceResponseDTO>.SuccessResult(
                        result, $"Attendance record {(isApproved ? "approved" : "rejected")}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error approving attendance record {Id}", id);
                    return ServiceResponse<AttendanceResponseDTO>.FromException(ex, "Failed to approve attendance record");
                }
            }, "ApproveAttendanceAsync");
        }

        #endregion

        #region Analytics and Calculations

        /// <inheritdoc />
        public async Task<ServiceResponse<decimal>> CalculateTotalHoursWorkedAsync(
            int employeeId, DateTime startDate, DateTime endDate)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    var total = await _queryRepository.GetTotalHoursWorkedByEmployeeAsync(
                        employeeId, startDate, endDate);

                    return ServiceResponse<decimal>.SuccessResult(total);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating total hours for employee {EmployeeId}", employeeId);
                    return ServiceResponse<decimal>.FromException(ex, "Failed to calculate total hours");
                }
            }, "CalculateTotalHoursWorkedAsync");
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<decimal>> CalculateTotalOvertimeAsync(
            int employeeId, DateTime startDate, DateTime endDate)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    var total = await _queryRepository.GetTotalOvertimeByEmployeeAsync(
                        employeeId, startDate, endDate);

                    return ServiceResponse<decimal>.SuccessResult(total);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating overtime for employee {EmployeeId}", employeeId);
                    return ServiceResponse<decimal>.FromException(ex, "Failed to calculate overtime");
                }
            }, "CalculateTotalOvertimeAsync");
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<double>> CalculateAttendancePercentageAsync(
            int employeeId, DateTime startDate, DateTime endDate)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    var attendance = await _queryRepository.GetEmployeeAttendanceByDateRangeAsync(
                        employeeId, startDate, endDate);

                    var workingDays = (endDate - startDate).Days + 1;
                    var presentDays = attendance.Count(a => a.Status == "Present" || a.Status == "Late");

                    var percentage = workingDays > 0
                        ? Math.Round((double)presentDays / workingDays * 100, 2)
                        : 0;
                    
                    return ServiceResponse<double>.SuccessResult(percentage);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating attendance percentage for employee {EmployeeId}", employeeId);
                    return ServiceResponse<double>.FromException(ex, "Failed to calculate attendance percentage");
                }
            }, "CalculateAttendancePercentageAsync");
        }

        #endregion

        #region Validation and Utilities

        /// <inheritdoc />
        public async Task<ServiceResponse<bool>> IsEmployeeClockedInTodayAsync(int employeeId)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    var today = DateTime.Today;
                    var attendance = await _queryRepository.GetAttendanceByEmployeeIdAsync(employeeId);
                    var todaysRecord = attendance.FirstOrDefault(a => a.AttendanceDate == today);

                    return ServiceResponse<bool>.SuccessResult(
                        todaysRecord != null && todaysRecord.TimeIn.HasValue && !todaysRecord.TimeOut.HasValue);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking clock-in status for employee {EmployeeId}", employeeId);
                    return ServiceResponse<bool>.FromException(ex, "Failed to check clock-in status");
                }
            }, "IsEmployeeClockedInTodayAsync");
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<string>> GetEmployeeCurrentStatusAsync(int employeeId)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    var today = DateTime.Today;
                    var attendance = await _queryRepository.GetAttendanceByEmployeeIdAsync(employeeId);
                    var todaysRecord = attendance.FirstOrDefault(a => a.AttendanceDate == today);

                    if (todaysRecord == null)
                        return ServiceResponse<string>.SuccessResult("Not Started");

                    if (todaysRecord.TimeIn.HasValue && !todaysRecord.TimeOut.HasValue)
                        return ServiceResponse<string>.SuccessResult("Clocked In");

                    if (todaysRecord.TimeIn.HasValue && todaysRecord.TimeOut.HasValue)
                        return ServiceResponse<string>.SuccessResult("Completed");

                    return ServiceResponse<string>.SuccessResult("Unknown");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting status for employee {EmployeeId}", employeeId);
                    return ServiceResponse<string>.FromException(ex, "Failed to get employee status");
                }
            }, "GetEmployeeCurrentStatusAsync");
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<bool>> ValidateLocationAsync(double latitude, double longitude)
        {
            return await ExecuteWithPerformanceTrackingAsync(async () =>
            {
                try
                {
                    var isValid = IsWithinGeofence(latitude, longitude);
                    return ServiceResponse<bool>.SuccessResult(isValid);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating location ({Lat}, {Lon})", latitude, longitude);
                    return ServiceResponse<bool>.FromException(ex, "Failed to validate location");
                }
            }, "ValidateLocationAsync");
        }

        #endregion

        #region Private Helper Methods

        private void CalculateHoursWorked(EMAttendance attendance)
        {
            if (attendance.TimeIn.HasValue && attendance.TimeOut.HasValue)
            {
                var timeIn = attendance.TimeIn.Value;
                var timeOut = attendance.TimeOut.Value;

                // Calculate total hours worked
                var hoursWorked = (timeOut - timeIn).TotalHours;
                attendance.HoursWorked = (decimal)Math.Max(0, hoursWorked);

                // Calculate overtime (hours beyond standard)
                if (attendance.HoursWorked > _settings.StandardHoursPerDay)
                {
                    attendance.OvertimeHours = attendance.HoursWorked - _settings.StandardHoursPerDay;
                }
                else
                {
                    attendance.OvertimeHours = 0;
                }
            }
        }

        private string DetermineClockInStatus(TimeSpan timeIn)
        {
            if (timeIn <= _settings.StandardShiftStart.Add(_settings.GracePeriod))
                return "Present";
            else if (timeIn <= _settings.LateThreshold)
                return "Late";
            else
                return "Late-Excessive";
        }

        private string DetermineClockOutStatus(EMAttendance attendance)
        {
            if (!attendance.TimeIn.HasValue) return "Invalid";

            if (attendance.HoursWorked >= _settings.StandardHoursPerDay)
                return "Completed";
            else if (attendance.HoursWorked >= _settings.StandardHoursPerDay - 1)
                return "Early Departure";
            else
                return "Partial Day";
        }

        private string DetermineShift(TimeSpan timeIn)
        {
            if (timeIn < new TimeSpan(12, 0, 0))
                return "Morning";
            else if (timeIn < new TimeSpan(17, 0, 0))
                return "Afternoon";
            else
                return "Evening";
        }

        private bool IsWithinGeofence(double latitude, double longitude)
        {
            // Haversine formula to calculate distance between two points
            const double R = 6371; // Earth's radius in kilometers

            var dLat = ToRadians(latitude - _settings.OfficeLocation.Lat);
            var dLon = ToRadians(longitude - _settings.OfficeLocation.Lon);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(_settings.OfficeLocation.Lat)) * Math.Cos(ToRadians(latitude)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var distance = R * c;

            return distance <= _settings.GeofenceRadiusKm;
        }

        private double ToRadians(double degrees) => degrees * Math.PI / 180;

        private byte[] GenerateCsvExport(List<EMAttendance> attendance)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Date,EmployeeID,EmployeeName,TimeIn,TimeOut,HoursWorked,Overtime,Status,Shift");

            foreach (var a in attendance.OrderBy(a => a.AttendanceDate).ThenBy(a => a.Employee?.LastName))
            {
                sb.AppendLine($"{a.AttendanceDate:yyyy-MM-dd}," +
                             $"{a.EmployeeId}," +
                             $"\"{a.Employee?.FirstName} {a.Employee?.LastName}\"," +
                             $"{a.TimeIn:hh\\:mm}," +
                             $"{a.TimeOut:hh\\:mm}," +
                             $"{a.HoursWorked:F2}," +
                             $"{a.OvertimeHours:F2}," +
                             $"{a.Status}," +
                             $"{a.Shift}");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private async Task<byte[]> GenerateExcelExport(List<EMAttendance> attendance, AttendanceExportRequestDTO request)
        {
            // Note: In production, use EPPlus or ClosedXML for proper Excel generation
            // This is a simplified version that returns CSV with .xlsx extension
            var csv = GenerateCsvExport(attendance);
            return csv;
        }

        private async Task<byte[]> GeneratePdfExport(List<EMAttendance> attendance, AttendanceExportRequestDTO request)
        {
            // Note: In production, use a PDF library like iTextSharp or QuestPDF
            // This is a placeholder
            var html = GenerateHtmlReport(attendance, request);
            return Encoding.UTF8.GetBytes(html);
        }

        private byte[] GenerateJsonExport(List<EMAttendance> attendance)
        {
            var dtos = _mapper.Map<IEnumerable<AttendanceResponseDTO>>(attendance);
            var json = JsonSerializer.Serialize(dtos, new JsonSerializerOptions { WriteIndented = true });
            return Encoding.UTF8.GetBytes(json);
        }

        private string GenerateHtmlReport(List<EMAttendance> attendance, AttendanceExportRequestDTO request)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><head><style>");
            sb.AppendLine("body { font-family: Arial; }");
            sb.AppendLine("table { border-collapse: collapse; width: 100%; }");
            sb.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            sb.AppendLine("th { background-color: #4CAF50; color: white; }");
            sb.AppendLine("</style></head><body>");

            sb.AppendLine($"<h2>Attendance Report</h2>");
            sb.AppendLine($"<p>Period: {request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd}</p>");
            sb.AppendLine($"<p>Generated: {DateTime.Now:yyyy-MM-dd HH:mm}</p>");

            if (!string.IsNullOrEmpty(request.Department))
            {
                sb.AppendLine($"<p>Department: {request.Department}</p>");
            }

            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Date</th><th>Employee</th><th>Time In</th><th>Time Out</th><th>Hours</th><th>Overtime</th><th>Status</th></tr>");

            foreach (var a in attendance.OrderBy(a => a.AttendanceDate).ThenBy(a => a.Employee?.LastName))
            {
                sb.AppendLine($"<tr>");
                sb.AppendLine($"<td>{a.AttendanceDate:yyyy-MM-dd}</td>");
                sb.AppendLine($"<td>{a.Employee?.FirstName} {a.Employee?.LastName}</td>");
                sb.AppendLine($"<td>{a.TimeIn:hh\\:mm}</td>");
                sb.AppendLine($"<td>{a.TimeOut:hh\\:mm}</td>");
                sb.AppendLine($"<td>{a.HoursWorked:F2}</td>");
                sb.AppendLine($"<td>{a.OvertimeHours:F2}</td>");
                sb.AppendLine($"<td>{a.Status}</td>");
                sb.AppendLine($"</tr>");
            }

            sb.AppendLine("</table>");
            sb.AppendLine("</body></html>");

            return sb.ToString();
        }

        #endregion
    }
}
