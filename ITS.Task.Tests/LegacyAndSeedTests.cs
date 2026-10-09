using System.Net.Http.Json;
using ITS_Task.Business.DTOs;
using ITS_Task.DataAccess;
using ITS_Task.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace ITS_Task.Tests;
public partial class ApiTests
{
    [Fact] public async Task LegacyEmployeeCanReceiveAnAccountWithoutLosingHistory()
    {
        int id;
        using(var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var department = await db.Departments.SingleAsync();
            var employee = new Employee { FullName = "Legacy Employee", Email = "legacy@its.com", DepartmentId = department.Id, HireDate = DateTime.UtcNow.Date, JobTitle = "Developer" }; db.Employees.Add(employee); await db.SaveChangesAsync(); id = employee.Id;
        }
        await Login("admin@its.com","Admin@123"); (await client.PostAsJsonAsync($"/api/employees/{id}/account",new { password = "Legacy@123" })).EnsureSuccessStatusCode();
        await Login("legacy@its.com","Legacy@123"); Assert.Equal(id,(await client.GetFromJsonAsync<EmployeeDto>("/api/employees/me"))!.EmployeeId);
        Assert.Equal(2,(await client.GetFromJsonAsync<List<LeaveBalanceDto>>("/api/leaverequests/my-balance"))!.Count);
    }
    [Fact] public async Task RenamingAnnualLeaveDoesNotBreakDashboardBalance()
    {
        await Login("admin@its.com","Admin@123"); var types = (await client.GetFromJsonAsync<List<LeaveTypeDto>>("/api/leavetypes"))!; var annual = types.Single(t => t.IsAnnual);
        (await client.PutAsJsonAsync($"/api/leavetypes/{annual.Id}",new { name = "Yearly Vacation", allowedDays = annual.AllowedDays })).EnsureSuccessStatusCode();
        await Login("employee@its.com","Demo@123"); Assert.Equal(21,(await client.GetFromJsonAsync<EmployeeDashboardDto>("/api/dashboard/employee"))!.AnnualLeaveBalance);
    }
    [Fact] public async Task DemoSeedCanBeRepeatedWithoutDuplicatingData()
    {
        using var scope = host.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await scope.ServiceProvider.GetRequiredService<SeedInitializer>().InitializeAsync(true);
        Assert.Equal(2,await db.Employees.CountAsync()); Assert.Equal(3,await db.Users.CountAsync()); Assert.Equal(4,await db.LeaveBalances.CountAsync());
    }
    [Fact] public async Task RepeatedDeactivationAuditsOnlyOnce()
    {
        await Login("admin@its.com","Admin@123"); var employees = (await client.GetFromJsonAsync<EmployeePagedResultDto>("/api/employees?search=Demo%20Employee"))!; var id = employees.Employees.Single().EmployeeId;
        (await client.PutAsync($"/api/employees/{id}/deactivate",null)).EnsureSuccessStatusCode(); (await client.PutAsync($"/api/employees/{id}/deactivate",null)).EnsureSuccessStatusCode();
        using var scope = host.Services.CreateScope(); Assert.Equal(1,await scope.ServiceProvider.GetRequiredService<AppDbContext>().AuditLogs.CountAsync(a => a.Action == "Employee Deactivated"));
    }
}
