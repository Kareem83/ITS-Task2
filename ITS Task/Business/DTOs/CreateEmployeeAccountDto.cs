using System.ComponentModel.DataAnnotations;
namespace ITS_Task.Business.DTOs;
public sealed class CreateEmployeeAccountDto
{
    [Required, StringLength(128, MinimumLength = 6)] public string Password { get; set; } = string.Empty;
}
