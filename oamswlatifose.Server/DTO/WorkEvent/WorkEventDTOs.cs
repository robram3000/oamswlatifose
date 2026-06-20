using System.ComponentModel.DataAnnotations;

namespace oamswlatifose.Server.DTO.WorkEvent
{
    public class CreateWorkEventDTO
    {
        [Required]
        public DateTime Date { get; set; }

        [Required]
        [MaxLength(30)]
        public string EventType { get; set; } // Holiday, DayOff, Closed

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }
    }

    public class WorkEventResponseDTO
    {
        public int Id { get; set; }
        public string Date { get; set; }
        public string EventType { get; set; }
        public string Name { get; set; }
        public int CreatedByUserId { get; set; }
    }
}
