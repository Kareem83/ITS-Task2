using System.ComponentModel.DataAnnotations;
namespace ITS_Task.Business.DTOs;
public sealed class DepartmentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
public sealed class SaveDepartmentDto
{
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
}
