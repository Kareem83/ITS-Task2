using System.ComponentModel.DataAnnotations;

namespace ITS_Task.Business.DTOs
{
    public class CreateLeaveTypeDto
    {
        public bool IsAnnual { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(0, 365)]
        public int AllowedDays { get; set; }
    }
}