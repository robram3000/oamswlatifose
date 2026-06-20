using oamswlatifose.Server.DTO.Schedule;
using oamswlatifose.Server.Model.occurance;

namespace oamswlatifose.Server.Services.Schedule.Interfaces
{
    /// <summary>
    /// Manages per-employee work schedules. The schedule's start time + grace window is the
    /// reference attendance is graded against (see <see cref="ComputeStatus"/>).
    /// </summary>
    public interface IWorkScheduleService
    {
        /// <summary>Gets the active schedule for an employee, or a failure result if none is set.</summary>
        Task<ServiceResponse<WorkScheduleDTO>> GetByEmployeeAsync(int employeeId);

        /// <summary>Lists every employee's active schedule (admin view).</summary>
        Task<ServiceResponse<List<WorkScheduleDTO>>> GetAllAsync();

        /// <summary>Creates or updates (upserts) the schedule for an employee.</summary>
        Task<ServiceResponse<WorkScheduleDTO>> SetAsync(SetWorkScheduleDTO dto);

        /// <summary>The raw active schedule entity (or null) — used by the clock-in verifier.</summary>
        Task<EMWorkSchedule> GetEntityAsync(int employeeId);

        /// <summary>
        /// Grades a clock-in against a schedule: "Present" if at/before start + grace, else "Late".
        /// When <paramref name="schedule"/> is null, falls back to the system default (09:00 + 5m).
        /// </summary>
        string ComputeStatus(EMWorkSchedule schedule, TimeSpan timeIn);
    }
}
