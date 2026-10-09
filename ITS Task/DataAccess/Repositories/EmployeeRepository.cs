using ITS_Task.Business.DTOs;
using ITS_Task.Business.Services;
using ITS_Task.DataAccess.Interfaces;
using ITS_Task.Models;
using Microsoft.EntityFrameworkCore;
namespace ITS_Task.DataAccess.Repositories;
public class EmployeeRepository(AppDbContext context) : IEmployeeRepository
{
    public async Task<(List<Employee> Employees, int TotalCount)> QueryAsync(EmployeeQueryDto options, int? scopeManagerId = null)
    {
        QueryRules.Validate(options, "id", "name", "hiredate", "department");
        var query = context.Employees.Include(e => e.Department).AsNoTracking().AsQueryable();
        if (scopeManagerId.HasValue) query = query.Where(e => e.ManagerId == scopeManagerId);
        if (options.DepartmentId.HasValue) query = query.Where(e => e.DepartmentId == options.DepartmentId);
        if (options.ManagerId.HasValue) query = query.Where(e => e.ManagerId == options.ManagerId);
        if (options.IsActive.HasValue) query = query.Where(e => e.IsActive == options.IsActive);
        if (!string.IsNullOrWhiteSpace(options.Role))
            query = query.Where(e => context.UserRoles.Join(context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name }).Any(r => r.UserId == e.UserId && r.Name == options.Role));
        if (!string.IsNullOrWhiteSpace(options.Search))
        {
            var search = options.Search.Trim();
            query = query.Where(e => e.FullName.Contains(search) || e.Email.Contains(search) || e.JobTitle.Contains(search));
        }
        var count = await query.CountAsync();
        var desc = options.SortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);
        IOrderedQueryable<Employee> ordered = options.SortBy.ToLowerInvariant() switch
        {
            "name" => desc ? query.OrderByDescending(e => e.FullName) : query.OrderBy(e => e.FullName),
            "hiredate" => desc ? query.OrderByDescending(e => e.HireDate) : query.OrderBy(e => e.HireDate),
            "department" => desc ? query.OrderByDescending(e => e.Department.Name) : query.OrderBy(e => e.Department.Name),
            _ => desc ? query.OrderByDescending(e => e.Id) : query.OrderBy(e => e.Id)
        };
        return (await ordered.ThenBy(e => e.Id).Skip((options.Page - 1) * options.PageSize).Take(options.PageSize).ToListAsync(), count);
    }
    public Task<(List<Employee> Employees, int TotalCount)> GetAllAsync(int? departmentId, int? managerId, bool? isActive, string? role, int page, int pageSize, string sortBy, string sortOrder)
        => QueryAsync(new() { DepartmentId = departmentId, ManagerId = managerId, IsActive = isActive, Role = role, Page = page, PageSize = pageSize, SortBy = sortBy, SortOrder = sortOrder });
    public Task<(List<Employee> Employees, int TotalCount)> GetTeamAsync(int managerId, int page, int pageSize, string sortBy, string sortOrder)
        => QueryAsync(new() { Page = page, PageSize = pageSize, SortBy = sortBy, SortOrder = sortOrder }, managerId);
    public Task<Employee?> GetByUserIdAsync(string userId) => context.Employees.Include(e => e.Department).FirstOrDefaultAsync(e => e.UserId == userId);
    public Task<Employee?> GetByIdAsync(int id) => context.Employees.Include(e => e.Department).FirstOrDefaultAsync(e => e.Id == id);
    public Task<Employee?> GetByEmailAsync(string email) => context.Employees.FirstOrDefaultAsync(e => e.Email == email);
    public Task<Department?> GetDepartementByIdAsync(int id) => context.Departments.FirstOrDefaultAsync(d => d.Id == id);
    public async Task<Employee> AddAsync(Employee employee)
    {
        context.Employees.Add(employee);
        await context.SaveChangesAsync();
        return employee;
    }
    public async Task<Employee> UpdateAsync(Employee employee)
    {
        context.Employees.Update(employee);
        await context.SaveChangesAsync();
        return employee;
    }
    public async Task DeleteAsync(Employee employee)
    {
        employee.IsActive = false;
        await UpdateAsync(employee);
    }
    public async Task<List<Employee>> SearchAsync(string search) => (await QueryAsync(new() { Search = search, PageSize = 100 })).Employees;
    public async Task<List<Employee>> SearchByManagerIdAsync(int managerId, string search) => (await QueryAsync(new() { Search = search, PageSize = 100 }, managerId)).Employees;
    public Task<List<Employee>> GetByManagerIdAsync(int managerId) => context.Employees.Include(e => e.Department).Where(e => e.ManagerId == managerId).OrderBy(e => e.Id).Take(100).ToListAsync();
}
