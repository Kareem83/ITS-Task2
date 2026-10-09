using System.ComponentModel.DataAnnotations;

namespace ITS_Task.Business.DTOs
{
    public class CreateLeaveRequestDto
    {
        [Required]
        public int LeaveTypeId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;
    }
}