using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using oamswlatifose.Server.DTO.WorkEvent;
using oamswlatifose.Server.Model;
using oamswlatifose.Server.Model.occurance;
using oamswlatifose.Server.Services;

namespace oamswlatifose.Server.Controllers
{
    /// <summary>
    /// API controller for company work-event management (holidays, special days, etc.).
    /// HR/Admin can create and delete events; all authenticated users can read them.
    ///
    /// <para>License: Proprietary software by Roberto V Ramirez Jr (robram3000@gmail.com).
    /// A valid license key is required after the 30-day trial. Day 31 and beyond will
    /// deny all requests until a license issued by robram3000@gmail.com is activated.</para>
    /// </summary>
    [ApiController]
    [Route("api/work-events")]
    [Authorize]
    public class WorkEventController : BaseApiController
    {
        private readonly ApplicationDbContext _db;

        public WorkEventController(ApplicationDbContext db) => _db = db;

        private string GetCurrentRoleName() => User.FindFirst("role_name")?.Value ?? "";
        private bool IsManagerRole() { var r = GetCurrentRoleName(); return r == "Admin" || r == "HR"; }

        /// <summary>Get work events for a given year/month (all authenticated users).</summary>
        [HttpGet]
        public async Task<IActionResult> GetByMonth([FromQuery] int year, [FromQuery] int month)
        {
            if (year < 2000 || year > 2100 || month < 1 || month > 12)
                return BadRequest(ServiceResponse<List<WorkEventResponseDTO>>.FailureResult("Invalid year or month"));

            var start = new DateTime(year, month, 1);
            var end = start.AddMonths(1).AddDays(-1);

            var events = await _db.EMWorkEvents
                .Where(e => e.Date >= start && e.Date <= end)
                .OrderBy(e => e.Date)
                .ToListAsync();

            return Ok(ServiceResponse<List<WorkEventResponseDTO>>.SuccessResult(events.Select(ToDto).ToList()));
        }

        /// <summary>Create a work event (HR/Admin only).</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateWorkEventDTO dto)
        {
            if (!IsManagerRole()) return Forbid();
            if (!ModelState.IsValid)
                return BadRequest(ServiceResponse<WorkEventResponseDTO>.FailureResult("Invalid request"));

            var validTypes = new[] { "Holiday", "DayOff", "Closed" };
            if (!validTypes.Contains(dto.EventType))
                return BadRequest(ServiceResponse<WorkEventResponseDTO>.FailureResult("EventType must be Holiday, DayOff, or Closed"));

            var ev = new EMWorkEvent
            {
                Date = dto.Date.Date,
                EventType = dto.EventType,
                Name = dto.Name,
                CreatedByUserId = GetCurrentUserId(),
                CreatedAt = DateTime.UtcNow,
            };

            _db.EMWorkEvents.Add(ev);
            await _db.SaveChangesAsync();

            return Ok(ServiceResponse<WorkEventResponseDTO>.SuccessResult(ToDto(ev), "Work event created"));
        }

        /// <summary>Delete a work event (HR/Admin only).</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsManagerRole()) return Forbid();

            var ev = await _db.EMWorkEvents.FindAsync(id);
            if (ev == null) return NotFound(ServiceResponse<bool>.FailureResult("Work event not found"));

            _db.EMWorkEvents.Remove(ev);
            await _db.SaveChangesAsync();

            return Ok(ServiceResponse<bool>.SuccessResult(true, "Work event deleted"));
        }

        private static WorkEventResponseDTO ToDto(EMWorkEvent e) => new()
        {
            Id = e.Id,
            Date = e.Date.ToString("yyyy-MM-dd"),
            EventType = e.EventType,
            Name = e.Name,
            CreatedByUserId = e.CreatedByUserId,
        };
    }
}
