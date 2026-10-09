namespace ITS_Task.Business.DTOs
{
    public class LeaveRequestDto
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public string EmployeeName { get; set; } = string.Empty;

        public int LeaveTypeId { get; set; }

        public string LeaveTypeName { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int Days { get; set; }

        public string Reason { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string? RejectReason { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}