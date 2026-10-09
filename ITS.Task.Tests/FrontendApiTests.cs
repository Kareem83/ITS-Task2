using System.Net;
using System.Net.Http.Json;
using ITS_Task.Business.DTOs;
namespace ITS_Task.Tests;
public partial class ApiTests
{
    [Fact] public async Task DirectoryIncludesAccountAndRoleInformation()
    {
        await Login("admin@its.com", "Admin@123");
        var page = (await client.GetFromJsonAsync<EmployeePagedResultDto>("/api/employees"))!;
        Assert.All(page.Employees, employee => Assert.True(employee.HasAccount));
        Assert.Contains("Manager", page.Employees.Single(e => e.Email == "manager@its.com").Roles);
        Assert.Contains("Employee", page.Employees.Single(e => e.Email == "employee@its.com").Roles);
    }
    [Fact] public async Task ManagerCanReadTeamDepartmentAndLeaveOptions()
    {
        await Login("manager@its.com", "Demo@123");
        var departments = (await client.GetFromJsonAsync<List<DepartmentDto>>("/api/departments/team-options"))!;
        Assert.Equal("Engineering", Assert.Single(departments).Name);
        Assert.Equal(2, (await client.GetFromJsonAsync<List<LeaveTypeDto>>("/api/leavetypes/options"))!.Count);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/departments")).StatusCode);
    }
    [Fact] public async Task EmployeeCannotReadManagerOptionsOrAuditHistory()
    {
        await Login("employee@its.com", "Demo@123");
        foreach (var path in new[] { "/api/departments/team-options", "/api/leavetypes/options", "/api/auditlogs/paged" })
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(path)).StatusCode);
    }
    [Fact] public async Task BalanceReportsInactivePoliciesWithoutLosingHistory()
    {
        await Login("admin@its.com", "Admin@123");
        var types = (await client.GetFromJsonAsync<List<LeaveTypeDto>>("/api/leavetypes"))!;
        (await client.PutAsync($"/api/leavetypes/{types[0].Id}/deactivate", null)).EnsureSuccessStatusCode();
        await Login("employee@its.com", "Demo@123");
        var balances = (await client.GetFromJsonAsync<List<LeaveBalanceDto>>("/api/leaverequests/my-balance"))!;
        Assert.Equal(2, balances.Count);
        Assert.False(balances.Single(b => b.LeaveTypeId == types[0].Id).IsActive);
    }
    [Fact] public async Task AuditPaginationSupportsActorSearchAndStableOrdering()
    {
        await Login("employee@its.com", "Demo@123");
        (await client.PostAsJsonAsync("/api/leaverequests", new { leaveTypeId = 1, startDate = "2027-03-07", endDate = "2027-03-07", reason = "Audit search" })).EnsureSuccessStatusCode();
        await Login("admin@its.com", "Admin@123");
        var logs = (await client.GetFromJsonAsync<PagedResultDto<AuditLogDto>>("/api/auditlogs/paged?search=Demo%20Employee&pageSize=1&sortBy=timestamp&sortOrder=desc"))!;
        Assert.True(logs.TotalCount > 0);
        Assert.Equal("Demo Employee", Assert.Single(logs.Items).UserName);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/auditlogs/paged?fromDate=2027-01-02&toDate=2027-01-01")).StatusCode);
    }
    [Fact] public async Task UnknownApiRouteDoesNotReturnSpaHtml()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/not-an-endpoint")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/health")).StatusCode);
    }
}
