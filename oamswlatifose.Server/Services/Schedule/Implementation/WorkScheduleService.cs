using Microsoft.EntityFrameworkCore;
using oamswlatifose.Server.DTO.Schedule;
using oamswlatifose.Server.Model;
using oamswlatifose.Server.Model.occurance;
using oamswlatifose.Server.Services.Schedule.Interfaces;
using System.Globalization;

namespace oamswlatifose.Server.Services.Schedule.Implementation
{
    /// <summary>
    /// EF-backed work-schedule store. Kept deliberately simple (talks to the DbContext
    /// directly) — it is a single small table with one active row per employee.
    /// </summary>
    public class WorkScheduleService : IWorkScheduleService
    {
        // System fallback when an employee has no schedule of their own.
        private static readonly TimeSpan DefaultStart = new(9, 0, 0);
        private const int DefaultGrace = 5;

        private readonly ApplicationDbContext _db;
        private readonly ILogger<WorkScheduleService> _logger;

        public WorkScheduleService(ApplicationDbContext db, ILogger<WorkScheduleService> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ServiceResponse<WorkScheduleDTO>> GetByEmployeeAsync(int employeeId)
        {
            try
            {
                var schedule = await _db.EMWorkSchedules
                    .Include(s => s.Employee)
                    .FirstOrDefaultAsync(s => s.EmployeeId == employeeId && s.IsActive);

                if (schedule == null)
                    return ServiceResponse<WorkScheduleDTO>.FailureResult("No schedule set for this employee");

                return ServiceResponse<WorkScheduleDTO>.SuccessResult(ToDto(schedule));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting schedule for employee {EmployeeId}", employeeId);
                return ServiceResponse<WorkScheduleDTO>.FromException(ex, "Failed to get schedule");
            }
        }

        public async Task<ServiceResponse<List<WorkScheduleDTO>>> GetAllAsync()
        {
            try
            {
                var schedules = await _db.EMWorkSchedules
                    .Include(s => s.Employee)
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.EmployeeId)
                    .ToListAsync();

                return ServiceResponse<List<WorkScheduleDTO>>.SuccessResult(schedules.Select(ToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing schedules");
                return ServiceResponse<List<WorkScheduleDTO>>.FromException(ex, "Failed to list schedules");
            }
        }

        public async Task<ServiceResponse<WorkScheduleDTO>> SetAsync(SetWorkScheduleDTO dto)
        {
            try
            {
                if (dto.EmployeeId is null or 0)
                    return ServiceResponse<WorkScheduleDTO>.FailureResult("Employee ID is required");

                var employee = await _db.EMEmployees.FirstOrDefaultAsync(e => e.Id == dto.EmployeeId);
                if (employee == null)
                    return ServiceResponse<WorkScheduleDTO>.FailureResult($"Employee {dto.EmployeeId} not found");

                if (!TryParseTime(dto.StartTime, out var start) || !TryParseTime(dto.EndTime, out var end))
                    return ServiceResponse<WorkScheduleDTO>.FailureResult("Start/End time must be HH:mm (24h)");

                if (end <= start)
                    return ServiceResponse<WorkScheduleDTO>.FailureResult("End time must be after start time");

                var schedule = await _db.EMWorkSchedules
                    .FirstOrDefaultAsync(s => s.EmployeeId == dto.EmployeeId);

                if (schedule == null)
                {
                    schedule = new EMWorkSchedule
                    {
                        EmployeeId = dto.EmployeeId.Value,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.EMWorkSchedules.Add(schedule);
                }
                else
                {
                    schedule.UpdatedAt = DateTime.UtcNow;
                }

                schedule.StartTime = start;
                schedule.EndTime = end;
                schedule.GraceMinutes = dto.GraceMinutes;
                schedule.WorkDays = string.IsNullOrWhiteSpace(dto.WorkDays) ? "Mon,Tue,Wed,Thu,Fri" : dto.WorkDays.Trim();
                schedule.IsActive = true;

                await _db.SaveChangesAsync();

                schedule.Employee = employee;
                _logger.LogInformation("Schedule set for employee {EmployeeId}: {Start}-{End} (+{Grace}m)",
                    schedule.EmployeeId, dto.StartTime, dto.EndTime, dto.GraceMinutes);

                return ServiceResponse<WorkScheduleDTO>.SuccessResult(ToDto(schedule), "Schedule saved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting schedule for employee {EmployeeId}", dto?.EmployeeId);
                return ServiceResponse<WorkScheduleDTO>.FromException(ex, "Failed to save schedule");
            }
        }

        public async Task<EMWorkSchedule> GetEntityAsync(int employeeId)
        {
            return await _db.EMWorkSchedules
                .FirstOrDefaultAsync(s => s.EmployeeId == employeeId && s.IsActive);
        }

        public string ComputeStatus(EMWorkSchedule schedule, TimeSpan timeIn)
        {
            var start = schedule?.StartTime ?? DefaultStart;
            var grace = schedule?.GraceMinutes ?? DefaultGrace;
            var lateAfter = start.Add(TimeSpan.FromMinutes(grace));
            return timeIn <= lateAfter ? "Present" : "Late";
        }

        private static WorkScheduleDTO ToDto(EMWorkSchedule s)
        {
            var lateAfter = s.StartTime.Add(TimeSpan.FromMinutes(s.GraceMinutes));
            return new WorkScheduleDTO
            {
                Id = s.Id,
                EmployeeId = s.EmployeeId,
                EmployeeName = s.Employee != null ? $"{s.Employee.FirstName} {s.Employee.LastName}" : null,
                StartTime = Fmt(s.StartTime),
                EndTime = Fmt(s.EndTime),
                GraceMinutes = s.GraceMinutes,
                WorkDays = s.WorkDays,
                IsActive = s.IsActive,
                LateAfter = Fmt(lateAfter)
            };
        }

        private static string Fmt(TimeSpan t) => t.ToString(@"hh\:mm", CultureInfo.InvariantCulture);

        private static bool TryParseTime(string value, out TimeSpan time)
        {
            time = default;
            if (string.IsNullOrWhiteSpace(value)) return false;
            return TimeSpan.TryParseExact(value.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out time);
        }
    }
}
