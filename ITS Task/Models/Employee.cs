using System.ComponentModel.DataAnnotations;

namespace ITS_Task.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public int DepartmentId { get; set; }

        public Department Department { get; set; } = null!;

        [MaxLength(100)]
        public string JobTitle { get; set; } = string.Empty;

        public DateTime HireDate { get; set; }

        public int? ManagerId { get; set; }

        public Employee? Manager { get; set; }

        public ICollection<Employee> Employees { get; set; }
            = new List<Employee>();

        public string? UserId { get; set; }

        public ApplicationUser? User { get; set; }

        public bool IsActive { get; set; } = true;
    }
}