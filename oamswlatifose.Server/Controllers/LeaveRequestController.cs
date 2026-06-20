using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using oamswlatifose.Server.DTO.Leave;
using oamswlatifose.Server.Model;
using oamswlatifose.Server.Model.occurance;
using oamswlatifose.Server.Services;

namespace oamswlatifose.Server.Controllers
{
    [ApiController]
    [Route("api/leave")]
    [Authorize]
    public class LeaveRequestController : BaseApiController
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<LeaveRequestController> _logger;

        public LeaveRequestController(ApplicationDbContext db, ILogger<LeaveRequestController> logger)
        {
            _db = db;
            _logger = logger;
        }

        private string GetCurrentRoleName() => User.FindFirst("role_name")?.Value ?? "";

        private bool IsManagerRole() { var r = GetCurrentRoleName(); return r == "Admin" || r == "HR"; }

        // ── Employee endpoints ────────────────────────────────────────

        /// <summary>Get the current employee's own leave requests.</summary>
        [HttpGet("mine")]
        public async Task<IActionResult> GetMine()
        {
            var empId = GetCurrentEmployeeId();
            if (empId == 0) return Ok(ServiceResponse<List<LeaveResponseDTO>>.SuccessResult([]));

            var rows = await _db.EMLeaveRequests
                .Where(r => r.EmployeeId == empId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(ServiceResponse<List<LeaveResponseDTO>>.SuccessResult(rows.Select(ToDto).ToList()));
        }

        /// <summary>Submit a new leave request.</summary>
        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] SubmitLeaveDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ServiceResponse<LeaveResponseDTO>.FailureResult("Invalid request"));

            var empId = GetCurrentEmployeeId();
            if (empId == 0)
                return BadRequest(ServiceResponse<LeaveResponseDTO>.FailureResult("No employee record linked to your account"));

            if (dto.EndDate < dto.StartDate)
                return BadRequest(ServiceResponse<LeaveResponseDTO>.FailureResult("End date must be on or after start date"));

            var leave = new EMLeaveRequest
            {
                EmployeeId = empId,
                StartDate = dto.StartDate.Date,
                EndDate = dto.EndDate.Date,
                LeaveType = dto.LeaveType,
                Reason = dto.Reason ?? "",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            _db.EMLeaveRequests.Add(leave);
            await _db.SaveChangesAsync();

            return Ok(ServiceResponse<LeaveResponseDTO>.SuccessResult(ToDto(leave), "Leave request submitted"));
        }

        /// <summary>Cancel a pending leave request (employee can only cancel their own Pending requests).</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Cancel(int id)
        {
            var empId = GetCurrentEmployeeId();
            var leave = await _db.EMLeaveRequests.FindAsync(id);
            if (leave == null) return NotFound(ServiceResponse<bool>.FailureResult("Leave request not found"));

            if (!IsManagerRole() && leave.EmployeeId != empId)
                return Forbid();

            if (!IsManagerRole() && leave.Status != "Pending")
                return BadRequest(ServiceResponse<bool>.FailureResult("Only pending requests can be cancelled"));

            _db.EMLeaveRequests.Remove(leave);
            await _db.SaveChangesAsync();

            return Ok(ServiceResponse<bool>.SuccessResult(true, "Leave request cancelled"));
        }

        // ── HR/Admin endpoints ─────────────────────────────────────────

        /// <summary>Get all leave requests (HR/Admin only).</summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAll([FromQuery] string status = null)
        {
            if (!IsManagerRole()) return Forbid();

            var query = _db.EMLeaveRequests
                .Include(r => r.Employee)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(r => r.Status == status);

            var rows = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            return Ok(ServiceResponse<List<LeaveResponseDTO>>.SuccessResult(rows.Select(ToDtoWithName).ToList()));
        }

        /// <summary>Approve or reject a leave request (HR/Admin only).</summary>
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> Approve(int id, [FromBody] ApproveLeaveDTO dto)
        {
            if (!IsManagerRole()) return Forbid();

            var leave = await _db.EMLeaveRequests.Include(r => r.Employee).FirstOrDefaultAsync(r => r.Id == id);
            if (leave == null) return NotFound(ServiceResponse<LeaveResponseDTO>.FailureResult("Leave request not found"));
            if (leave.Status != "Pending") return BadRequest(ServiceResponse<LeaveResponseDTO>.FailureResult("Only pending requests can be approved/rejected"));

            leave.Status = dto.IsApproved ? "Approved" : "Rejected";
            leave.ApprovedByUserId = GetCurrentUserId();
            leave.ApprovalNote = dto.Note ?? "";
            leave.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            var action = dto.IsApproved ? "approved" : "rejected";
            return Ok(ServiceResponse<LeaveResponseDTO>.SuccessResult(ToDtoWithName(leave), $"Leave request {action}"));
        }

        private static LeaveResponseDTO ToDto(EMLeaveRequest r) => new()
        {
            Id = r.Id,
            EmployeeId = r.EmployeeId,
            EmployeeName = r.Employee != null ? $"{r.Employee.FirstName} {r.Employee.LastName}" : null,
            StartDate = r.StartDate.ToString("yyyy-MM-dd"),
            EndDate = r.EndDate.ToString("yyyy-MM-dd"),
            LeaveType = r.LeaveType,
            Reason = r.Reason,
            Status = r.Status,
            ApprovalNote = r.ApprovalNote,
            CreatedAt = r.CreatedAt.ToString("yyyy-MM-dd"),
        };

        private static LeaveResponseDTO ToDtoWithName(EMLeaveRequest r) => ToDto(r);
    }
}
