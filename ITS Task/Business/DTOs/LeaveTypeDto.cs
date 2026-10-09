namespace ITS_Task.Business.DTOs
{
    public class LeaveTypeDto
    {
        public bool IsAnnual { get; set; }
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int AllowedDays { get; set; }

        public bool IsActive { get; set; }
    }
}