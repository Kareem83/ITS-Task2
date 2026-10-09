namespace ITS_Task.Business.DTOs
{
    public class EmployeePagedResultDto
    {
        public List<EmployeeDto> Employees { get; set; } = new();

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }
    }
}