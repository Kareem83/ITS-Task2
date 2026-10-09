using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITS_Task.Models
{
    public class LeaveBalance
    {
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public int LeaveTypeId { get; set; }

        [Required]
        [Range(0, 365)]
        public int AllocatedDays { get; set; }

        [Required]
        [Range(0, 365)]
        public int UsedDays { get; set; }

        [Required]
        [Range(0, 365)]
        public int PendingDays { get; set; }
        [NotMapped]
        public int RemainingDays =>
            AllocatedDays - UsedDays - PendingDays;

        public Employee Employee { get; set; } = null!;

        public LeaveType LeaveType { get; set; } = null!;
    }
}