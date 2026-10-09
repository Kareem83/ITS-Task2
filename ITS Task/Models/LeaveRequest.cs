using System.ComponentModel.DataAnnotations;

namespace ITS_Task.Models
{
    public class LeaveRequest
    {
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public int LeaveTypeId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        [Range(1, 365)]
        public int Days { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        [Required]
        public LeaveRequestStatus Status { get; set; }

        [StringLength(500)]
        public string? RejectReason { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public Employee Employee { get; set; } = null!;

        public LeaveType LeaveType { get; set; } = null!;
    }
}