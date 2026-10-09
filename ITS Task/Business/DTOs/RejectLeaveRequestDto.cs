using System.ComponentModel.DataAnnotations;

namespace ITS_Task.Business.DTOs
{
    public class RejectLeaveRequestDto
    {
        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;
    }
}