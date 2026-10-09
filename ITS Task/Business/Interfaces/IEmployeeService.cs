using ITS_Task.Business.DTOs;
using ITS_Task.Models;
using System.Security.Claims;

namespace ITS_Task.Business.Interfaces
{
    public interface IEmployeeService
    {
        Task<EmployeeDto?> CreateAccountAsync(int id, CreateEmployeeAccountDto dto);
        Task<EmployeeDto> GetMyProfileAsync(ClaimsPrincipal user);
        Task<EmployeePagedResultDto> QueryAsync(EmployeeQueryDto query, ClaimsPrincipal? manager = null);

        Task<List<EmployeeDto>> SearchMyTeamAsync(
    ClaimsPrincipal user,
    string search);

        Task<EmployeePagedResultDto> GetMyTeamAsync(
     ClaimsPrincipal user,
     int page,
     int pageSize,
     string sortBy,
     string sortOrder);

        Task<EmployeePagedResultDto> GetAllAsync(
     int? departmentId,
     int? managerId,
     bool? isActive,
     string? role,
     int page,
     int pageSize,
     string sortBy,
     string sortOrder);

        Task<EmployeeDto> AddAsync(CreateEmployeeDto employeeDto);

        Task<EmployeeDto?> GetByIdAsync(int id);


        Task<EmployeeDto?> UpdateAsync(int id, UpdateEmployeeDto employeeDto);

        Task<bool> DeleteAsync(int id);

        Task<List<EmployeeDto>> SearchAsync(string searchTerm);

        Task<bool> DeactivateAsync(int id);

        Task<EmployeeDto?> AssignDepartementToEmployee(int id, int DepartementId);


        Task<EmployeeDto?> AssignManagerToEmployeeAsync(
    int id,
    int managerId);


        Task<bool> AssignRoleToEmployeeAsync(
    int id,
    string role);
    }
}
