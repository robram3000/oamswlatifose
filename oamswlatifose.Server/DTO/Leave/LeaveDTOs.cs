using System.ComponentModel.DataAnnotations;

namespace oamswlatifose.Server.DTO.Leave
{
    public class SubmitLeaveDTO
    {
        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string LeaveType { get; set; }

        [MaxLength(500)]
        public string Reason { get; set; }
    }

    public class LeaveResponseDTO
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string LeaveType { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
        public string ApprovalNote { get; set; }
        public string CreatedAt { get; set; }
    }

    public class ApproveLeaveDTO
    {
        [Required]
        public bool IsApproved { get; set; }

        [MaxLength(500)]
        public string Note { get; set; }
    }
}
