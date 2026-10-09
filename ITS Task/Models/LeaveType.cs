using System.ComponentModel.DataAnnotations;

namespace ITS_Task.Models
{
    public class LeaveType
    {
        public int Id { get; set; }

        public bool IsAnnual { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(0, 365)]
        public int AllowedDays { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        public ICollection<LeaveRequest> LeaveRequests { get; set; }
            = new List<LeaveRequest>();

        public ICollection<LeaveBalance> LeaveBalances { get; set; }
            = new List<LeaveBalance>();
    }
}