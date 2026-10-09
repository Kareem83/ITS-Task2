namespace ITS_Task.Business.DTOs
{
    public class EmployeeDashboardDto
    {
        public int AnnualLeaveBalance { get; set; }

        public int PendingRequests { get; set; }

        public int ApprovedThisYear { get; set; }

        public int RejectedRequests { get; set; }
    }

    public class ManagerDashboardDto
    {
        public int TeamMembers { get; set; }

        public int PendingRequests { get; set; }

        public int ApprovedThisMonth { get; set; }

        public int EmployeesCurrentlyAway { get; set; }
    }

    public class AdminDashboardDto
    {
        public int TotalEmployees { get; set; }

        public int ActiveEmployees { get; set; }

        public int Departments { get; set; }

        public int PendingLeaveRequests { get; set; }

        public int ApprovedThisMonth { get; set; }
    }
}