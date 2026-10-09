namespace ITS_Task.Business.DTOs
{
    public class EmployeeDto
    {
        public bool IsActive { get; set; }
        public bool HasAccount { get; set; }
        public List<string> Roles { get; set; } = [];
        public int? ManagerId { get; set; }
        public int EmployeeId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public int DepartmentId { get; set; }

        public string DepartmentName { get; set; } = string.Empty;

        public string JobTitle { get; set; } = string.Empty;

        public DateTime HireDate { get; set; }
    }
}