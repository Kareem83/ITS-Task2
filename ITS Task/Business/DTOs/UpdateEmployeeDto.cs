using System.ComponentModel.DataAnnotations;

namespace ITS_Task.Business.DTOs
{
    public class UpdateEmployeeDto
    {


        [Required]
        [StringLength(100)]
    public string FullName { get; set; } = string.Empty;


        [Required]
        [EmailAddress]
        [StringLength(150)]
    public string Email { get; set; } = string.Empty;


        [Required]
        [Range(1, int.MaxValue)]
    public int DepartmentId { get; set; } 


        [Required]
        [StringLength(100)]
    public string JobTitle { get; set; } = string.Empty;



        [Required]
        public DateTime HireDate { get; set; }

       
    }
}