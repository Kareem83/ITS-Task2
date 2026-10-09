using ITS_Task.Business.DTOs;
using System.Security.Claims;
namespace ITS_Task.Business.Interfaces;
public interface IDepartmentService
{
    Task<List<DepartmentDto>> GetAllAsync();
    Task<List<DepartmentDto>> GetTeamOptionsAsync(ClaimsPrincipal user);
    Task<DepartmentDto?> GetByIdAsync(int id);
    Task<DepartmentDto> CreateAsync(SaveDepartmentDto dto);
    Task<DepartmentDto?> UpdateAsync(int id, SaveDepartmentDto dto);
    Task<bool> DeleteAsync(int id);
}
