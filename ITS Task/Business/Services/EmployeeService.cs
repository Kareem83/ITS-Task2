using ITS_Task.Business.DTOs;
using ITS_Task.Business.Exceptions;
using ITS_Task.Business.Interfaces;
using ITS_Task.DataAccess;
using ITS_Task.DataAccess.Interfaces;
using ITS_Task.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace ITS_Task.Business.Services;
public class EmployeeService(IEmployeeRepository repository, IAuditLogService audit, UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles, AppDbContext context, ITransactionRunner transactions) : IEmployeeService
{
    private static EmployeeDto Map(Employee employee) => new()
    {
        EmployeeId = employee.Id, FullName = employee.FullName, Email = employee.Email,
        DepartmentId = employee.DepartmentId, DepartmentName = employee.Department.Name,
        JobTitle = employee.JobTitle, HireDate = employee.HireDate, ManagerId = employee.ManagerId, IsActive = employee.IsActive, HasAccount = employee.UserId != null
    };
    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded) throw new ApiException(result.Errors.Any(e => e.Code == "ConcurrencyFailure") ? 409 : 400, string.Join("; ", result.Errors.Select(e => e.Description)));
    }
    private static void Validate(string name, string email, string title, DateTime hireDate)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100 || string.IsNullOrWhiteSpace(email) || email.Length > 150 || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email) || string.IsNullOrWhiteSpace(title) || title.Trim().Length > 100)
            throw new ApiException(400, "Invalid employee details");
        if (hireDate == DateTime.MinValue || hireDate.Date > DateTime.UtcNow.Date) throw new ApiException(400, "Invalid hire date");
    }
    public async Task<EmployeeDto> GetMyProfileAsync(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new ApiException(401, "Authentication required");
        var employee = await repository.GetByUserIdAsync(id) ?? throw new ApiException(404, "Employee profile not found");
        if (!employee.IsActive) throw new ApiException(403, "Employee is inactive");
        return Map(employee);
    }
    public async Task<EmployeePagedResultDto> QueryAsync(EmployeeQueryDto query, ClaimsPrincipal? manager = null)
    {
        int? managerId = null;
        if (manager != null) managerId = (await GetMyProfileAsync(manager)).EmployeeId;
        if (!string.IsNullOrWhiteSpace(query.Role) && !new[] { "Administrator", "Manager", "Employee" }.Contains(query.Role)) throw new ApiException(400, "Invalid role");
        var result = await repository.QueryAsync(query, managerId);
        var ids = result.Employees.Select(e => e.UserId).Where(id => id != null).ToList();
        var memberships = await context.UserRoles.Where(ur => ids.Contains(ur.UserId)).Join(context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name }).ToListAsync();
        var dtos = result.Employees.Select(e => { var dto = Map(e); dto.Roles = memberships.Where(m => m.UserId == e.UserId && m.Name != null).Select(m => m.Name!).OrderBy(r => r).ToList(); return dto; }).ToList();
        return new() { Employees = dtos, Page = query.Page, PageSize = query.PageSize, TotalCount = result.TotalCount, TotalPages = (int)Math.Ceiling(result.TotalCount / (double)query.PageSize) };
    }
    public Task<EmployeePagedResultDto> GetAllAsync(int? departmentId, int? managerId, bool? isActive, string? role, int page, int pageSize, string sortBy, string sortOrder)
        => QueryAsync(new() { DepartmentId = departmentId, ManagerId = managerId, IsActive = isActive, Role = role, Page = page, PageSize = pageSize, SortBy = sortBy, SortOrder = sortOrder });
    public Task<EmployeePagedResultDto> GetMyTeamAsync(ClaimsPrincipal user, int page, int pageSize, string sortBy, string sortOrder)
        => QueryAsync(new() { Page = page, PageSize = pageSize, SortBy = sortBy, SortOrder = sortOrder }, user);
    public async Task<List<EmployeeDto>> SearchAsync(string search) => (await QueryAsync(new() { Search = search, PageSize = 100 })).Employees;
    public async Task<List<EmployeeDto>> SearchMyTeamAsync(ClaimsPrincipal user, string search) => (await QueryAsync(new() { Search = search, PageSize = 100 }, user)).Employees;
    public async Task<EmployeeDto?> GetByIdAsync(int id)
    {
        var employee = await repository.GetByIdAsync(id);
        return employee == null ? null : Map(employee);
    }
    public Task<EmployeeDto?> CreateAccountAsync(int id, CreateEmployeeAccountDto dto) => transactions.RunAsync<EmployeeDto?>(async () =>
    {
        var employee = await repository.GetByIdAsync(id);
        if (employee == null) return null;
        if (!employee.IsActive || employee.UserId != null) throw new ApiException(409, "Employee is inactive or already linked to an account");
        if (await users.FindByEmailAsync(employee.Email) != null) throw new ApiException(409, "An account already exists with this email; resolve the legacy link before creating an account");
        if (!await roles.RoleExistsAsync("Employee")) throw new ApiException(409, "Employee role has not been initialized");
        var account = new ApplicationUser { UserName = employee.Email, Email = employee.Email, EmailConfirmed = true };
        Check(await users.CreateAsync(account, dto.Password)); Check(await users.AddToRoleAsync(account, "Employee"));
        employee.UserId = account.Id;
        var types = await context.LeaveTypes.Where(t => t.IsActive && !context.LeaveBalances.Any(b => b.EmployeeId == id && b.LeaveTypeId == t.Id)).ToListAsync();
        context.LeaveBalances.AddRange(types.Select(t => new LeaveBalance { EmployeeId = id, LeaveTypeId = t.Id, AllocatedDays = t.AllowedDays }));
        await repository.UpdateAsync(employee);
        await audit.LogAsync("Employee Updated", "Employee", id.ToString(), "Employee account created");
        return Map(employee);
    });
    public Task<EmployeeDto> AddAsync(CreateEmployeeDto dto) => transactions.RunAsync(async () =>
    {
        Validate(dto.FullName, dto.Email, dto.JobTitle, dto.HireDate);
        var email = dto.Email.Trim();
        var department = await repository.GetDepartementByIdAsync(dto.DepartmentId) ?? throw new ApiException(404, "Department not found");
        if (await context.Employees.AnyAsync(e => e.Email.ToUpper() == email.ToUpper()) || await users.FindByEmailAsync(email) != null) throw new ApiException(409, "Email already exists");
        if (!await roles.RoleExistsAsync("Employee")) throw new ApiException(409, "Employee role has not been initialized");
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        Check(await users.CreateAsync(user, dto.Password));
        Check(await users.AddToRoleAsync(user, "Employee"));
        var employee = new Employee { FullName = dto.FullName.Trim(), Email = email, JobTitle = dto.JobTitle.Trim(), HireDate = dto.HireDate.Date, DepartmentId = department.Id, Department = department, UserId = user.Id };
        await repository.AddAsync(employee);
        var types = await context.LeaveTypes.Where(t => t.IsActive).ToListAsync();
        context.LeaveBalances.AddRange(types.Select(t => new LeaveBalance { EmployeeId = employee.Id, LeaveTypeId = t.Id, AllocatedDays = t.AllowedDays }));
        await context.SaveChangesAsync();
        await audit.LogAsync("Employee Created", "Employee", employee.Id.ToString());
        return Map(employee);
    });
    public Task<EmployeeDto?> UpdateAsync(int id, UpdateEmployeeDto dto) => transactions.RunAsync<EmployeeDto?>(async () =>
    {
        Validate(dto.FullName, dto.Email, dto.JobTitle, dto.HireDate);
        var employee = await repository.GetByIdAsync(id);
        if (employee == null) return null;
        var email = dto.Email.Trim();
        var department = await repository.GetDepartementByIdAsync(dto.DepartmentId) ?? throw new ApiException(404, "Department not found");
        if (await context.Employees.AnyAsync(e => e.Id != id && e.Email.ToUpper() == email.ToUpper())) throw new ApiException(409, "Email already exists");
        var existingUser = await users.FindByEmailAsync(email);
        if (existingUser != null && existingUser.Id != employee.UserId) throw new ApiException(409, "Email already exists");
        if (employee.UserId != null)
        {
            var user = await users.FindByIdAsync(employee.UserId) ?? throw new ApiException(409, "Employee account not found");
            user.Email = email;
            user.UserName = email;
            Check(await users.UpdateAsync(user));
        }
        employee.FullName = dto.FullName.Trim(); employee.Email = email; employee.JobTitle = dto.JobTitle.Trim(); employee.HireDate = dto.HireDate.Date; employee.DepartmentId = department.Id; employee.Department = department;
        await repository.UpdateAsync(employee);
        await audit.LogAsync("Employee Updated", "Employee", id.ToString());
        return Map(employee);
    });
    public Task<bool> DeleteAsync(int id) => DeactivateAsync(id);
    public Task<bool> DeactivateAsync(int id) => transactions.RunAsync(async () =>
    {
        var employee = await repository.GetByIdAsync(id);
        if (employee == null) return false;
        if (!employee.IsActive) return true;
        employee.IsActive = false;
        if (employee.UserId != null)
        {
            var user = await users.FindByIdAsync(employee.UserId);
            if (user != null) { user.RefreshToken = null; user.RefreshTokenExpiryTime = null; Check(await users.UpdateAsync(user)); }
        }
        await repository.UpdateAsync(employee);
        await audit.LogAsync("Employee Deactivated", "Employee", id.ToString());
        return true;
    });
    public Task<EmployeeDto?> AssignDepartementToEmployee(int id, int DepartementId) => transactions.RunAsync<EmployeeDto?>(async () =>
    {
        var employee = await repository.GetByIdAsync(id);
        if (employee == null) return null;
        var department = await repository.GetDepartementByIdAsync(DepartementId) ?? throw new ApiException(404, "Department not found");
        employee.DepartmentId = department.Id; employee.Department = department;
        await repository.UpdateAsync(employee);
        return Map(employee);
    });
    public Task<EmployeeDto?> AssignManagerToEmployeeAsync(int id, int managerId) => transactions.RunAsync<EmployeeDto?>(async () =>
    {
        var employee = await repository.GetByIdAsync(id);
        if (employee == null) return null;
        if (managerId == 0) { employee.ManagerId = null; await repository.UpdateAsync(employee); return Map(employee); }
        var manager = await repository.GetByIdAsync(managerId) ?? throw new ApiException(404, "Manager not found");
        if (!manager.IsActive || manager.UserId == null) throw new ApiException(400, "Manager must have an active account");
        var account = await users.FindByIdAsync(manager.UserId);
        if (account == null || !await users.IsInRoleAsync(account, "Manager")) throw new ApiException(400, "Assigned employee must have the Manager role");
        var seen = new HashSet<int> { employee.Id };
        Employee? current = manager;
        while (current != null)
        {
            if (!seen.Add(current.Id)) throw new ApiException(409, "Manager assignment creates a reporting cycle");
            current = current.ManagerId.HasValue ? await repository.GetByIdAsync(current.ManagerId.Value) : null;
        }
        employee.ManagerId = manager.Id;
        await repository.UpdateAsync(employee);
        return Map(employee);
    });
    public Task<bool> AssignRoleToEmployeeAsync(int id, string role) => transactions.RunAsync(async () =>
    {
        if (!new[] { "Administrator", "Manager", "Employee" }.Contains(role) || !await roles.RoleExistsAsync(role)) throw new ApiException(400, "Invalid role");
        var employee = await repository.GetByIdAsync(id);
        if (employee == null || employee.UserId == null) return false;
        if (!employee.IsActive) throw new ApiException(409, "Employee is inactive");
        if (role != "Manager" && await context.Employees.AnyAsync(e => e.ManagerId == id)) throw new ApiException(409, "Reassign this manager's team before changing the role");
        var user = await users.FindByIdAsync(employee.UserId);
        if (user == null) return false;
        var existing = await users.GetRolesAsync(user);
        if (existing.Count == 1 && existing.Contains(role)) return true;
        if (existing.Any()) Check(await users.RemoveFromRolesAsync(user, existing));
        Check(await users.AddToRoleAsync(user, role));
        user.RefreshToken = null; user.RefreshTokenExpiryTime = null;
        Check(await users.UpdateAsync(user));
        await audit.LogAsync("Role Changed", "User", user.Id, $"New role: {role}");
        return true;
    });
}
