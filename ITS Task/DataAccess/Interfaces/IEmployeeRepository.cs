using ITS_Task.Business.DTOs;
using ITS_Task.Models;

namespace ITS_Task.DataAccess.Interfaces
{
    public interface IEmployeeRepository
    {
        Task<(List<Employee> Employees, int TotalCount)> QueryAsync(EmployeeQueryDto query, int? scopeManagerId = null);

        Task<(List<Employee> Employees, int TotalCount)> GetTeamAsync(
    int managerId,
    int page,
    int pageSize,
    string sortBy,
    string sortOrder);

        Task<List<Employee>> SearchByManagerIdAsync(
    int managerId,
    string search);

        Task<Employee?> GetByUserIdAsync(string userId);

        Task<List<Employee>> GetByManagerIdAsync(int managerId);
        Task<(List<Employee> Employees, int TotalCount)> GetAllAsync(
     int? departmentId,
     int? managerId,
     bool? isActive,
     string? role,
     int page,
     int pageSize,
     string sortBy,
     string sortOrder);
        Task<Employee> AddAsync(Employee employee);

        Task<Employee?> GetByIdAsync(int id);

        Task<Employee> UpdateAsync(Employee employee);

        Task DeleteAsync(Employee employee);

        Task<List<Employee>> SearchAsync(string searchTerm);


        Task<Employee?> GetByEmailAsync(string email);

        Task<Department?> GetDepartementByIdAsync(int id);

    }

}
