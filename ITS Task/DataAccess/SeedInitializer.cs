using ITS_Task.Business.Exceptions;
using ITS_Task.Business.Interfaces;
using ITS_Task.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace ITS_Task.DataAccess;
public sealed class SeedInitializer(AppDbContext context, UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles, IConfiguration configuration, ITransactionRunner transactions)
{
    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
    public Task<bool> InitializeAsync(bool includeDemo) => transactions.RunAsync(async () =>
    {
        foreach (var name in new[] { "Administrator", "Manager", "Employee" })
            if (!await roles.RoleExistsAsync(name)) Check(await roles.CreateAsync(new IdentityRole(name)));
        var email = configuration["Seed:AdminEmail"] ?? "admin@its.com";
        var admin = await users.FindByEmailAsync(email);
        if (admin == null)
        {
            var password = configuration["Seed:AdminPassword"] ?? throw new InvalidOperationException("Configure Seed:AdminPassword to initialize the administrator");
            admin = new ApplicationUser { Email = email, UserName = email, EmailConfirmed = true };
            Check(await users.CreateAsync(admin, password));
        }
        if (!await users.IsInRoleAsync(admin, "Administrator")) Check(await users.AddToRoleAsync(admin, "Administrator"));
        if (!includeDemo) return true;
        var existingDemo = await context.Employees.Include(e => e.User).Where(e => e.Email == "manager@its.com" || e.Email == "employee@its.com").ToListAsync();
        if (existingDemo.Count == 2 && existingDemo.All(e => e.User != null) && await context.Departments.AnyAsync(d => d.Name == "Engineering") && await context.LeaveTypes.AnyAsync(t => t.Name == "Annual Leave") && await context.LeaveTypes.AnyAsync(t => t.Name == "Sick Leave")) return true;
        if (await context.Employees.AnyAsync() || await context.Departments.AnyAsync() || await context.LeaveTypes.AnyAsync())
            throw new ApiException(409, "Demo seeding requires an empty application database");
        var demoPassword = configuration["Seed:DemoPassword"] ?? throw new InvalidOperationException("Configure Seed:DemoPassword");
        var department = new Department { Name = "Engineering" };
        var annual = new LeaveType { Name = "Annual Leave", AllowedDays = 21, IsAnnual = true };
        var sick = new LeaveType { Name = "Sick Leave", AllowedDays = 10 };
        context.Departments.Add(department); context.LeaveTypes.AddRange(annual, sick);
        await context.SaveChangesAsync();
        var manager = await CreateEmployee("Demo Manager", "manager@its.com", "Manager", department, demoPassword);
        var employee = await CreateEmployee("Demo Employee", "employee@its.com", "Employee", department, demoPassword);
        employee.ManagerId = manager.Id;
        foreach (var person in new[] { manager, employee })
            foreach (var type in new[] { annual, sick }) context.LeaveBalances.Add(new LeaveBalance { EmployeeId = person.Id, LeaveTypeId = type.Id, AllocatedDays = type.AllowedDays });
        await context.SaveChangesAsync();
        return true;
    });
    private async Task<Employee> CreateEmployee(string name, string email, string role, Department department, string password)
    {
        var account = new ApplicationUser { Email = email, UserName = email, EmailConfirmed = true };
        Check(await users.CreateAsync(account, password)); Check(await users.AddToRoleAsync(account, role));
        var employee = new Employee { FullName = name, Email = email, UserId = account.Id, DepartmentId = department.Id, JobTitle = role, HireDate = DateTime.UtcNow.Date };
        context.Employees.Add(employee); await context.SaveChangesAsync(); return employee;
    }
}
