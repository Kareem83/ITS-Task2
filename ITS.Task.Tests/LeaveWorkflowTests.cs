using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using ITS_Task.Business.DTOs;
using ITS_Task.Business.Exceptions;
using ITS_Task.Business.Interfaces;
using ITS_Task.Business.Services;
using ITS_Task.DataAccess;
using ITS_Task.DataAccess.Interfaces;
using ITS_Task.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace ITS_Task.Tests;
public partial class ApiTests
{
    private async Task<LeaveRequestDto> Submit(string start = "2026-11-01", string end = "2026-11-03", string reason = "Vacation")
    {
        await Login("employee@its.com", "Demo@123");
        var balances = (await client.GetFromJsonAsync<List<LeaveBalanceDto>>("/api/leaverequests/my-balance"))!;
        var response = await client.PostAsJsonAsync("/api/leaverequests", new { leaveTypeId = balances.Single(b => b.LeaveTypeName == "Annual Leave").LeaveTypeId, startDate = DateTime.Parse(start), endDate = DateTime.Parse(end), reason });
        response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<LeaveRequestDto>())!;
    }
    private async Task<LeaveBalanceDto> AnnualBalance()
    {
        await Login("employee@its.com", "Demo@123");
        return (await client.GetFromJsonAsync<List<LeaveBalanceDto>>("/api/leaverequests/my-balance"))!.Single(b => b.LeaveTypeName == "Annual Leave");
    }
    [Fact] public async Task SubmissionReservesBalanceAndNormalizesDates()
    {
        var request = await Submit("2026-11-01T18:00:00", "2026-11-03T01:00:00"); var balance = await AnnualBalance();
        Assert.Equal(3, balance.PendingDays); Assert.Equal(0, balance.UsedDays); Assert.Equal(18, balance.RemainingDays);
        Assert.Equal(TimeSpan.Zero, request.StartDate.TimeOfDay); Assert.Equal(TimeSpan.Zero, request.EndDate.TimeOfDay);
    }
    [Fact] public async Task ApprovalMovesPendingToUsedAndAudits()
    {
        var request = await Submit(); await Login("manager@its.com", "Demo@123");
        (await client.PutAsync($"/api/leaverequests/{request.Id}/approve", null)).EnsureSuccessStatusCode();
        var balance = await AnnualBalance(); Assert.Equal(0, balance.PendingDays); Assert.Equal(3, balance.UsedDays); Assert.Equal(18, balance.RemainingDays);
        await Login("admin@its.com", "Admin@123"); var audit = (await client.GetFromJsonAsync<List<AuditLogDto>>("/api/auditlogs"))!;
        Assert.Single(audit, a => a.Action == "Leave Submitted"); Assert.Single(audit, a => a.Action == "Leave Approved");
    }
    [Fact] public async Task RejectionRestoresReservationAndStoresReason()
    {
        var request = await Submit(); await Login("manager@its.com", "Demo@123");
        var response = await client.PutAsJsonAsync($"/api/leaverequests/{request.Id}/reject", new { reason = "Insufficient coverage" }); response.EnsureSuccessStatusCode();
        Assert.Equal("Insufficient coverage", (await response.Content.ReadFromJsonAsync<LeaveRequestDto>())!.RejectReason);
        var balance = await AnnualBalance(); Assert.Equal(21, balance.RemainingDays); Assert.Equal(0, balance.PendingDays); Assert.Equal(0, balance.UsedDays);
    }
    [Fact] public async Task CancellationRestoresReservation()
    {
        var request = await Submit(); (await client.PutAsync($"/api/leaverequests/{request.Id}/cancel", null)).EnsureSuccessStatusCode();
        Assert.Equal(21, (await AnnualBalance()).RemainingDays);
    }
    [Fact] public async Task DuplicateApprovalReturnsConflictWithoutDoubleDeduction()
    {
        var request = await Submit(); await Login("manager@its.com", "Demo@123");
        (await client.PutAsync($"/api/leaverequests/{request.Id}/approve", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsync($"/api/leaverequests/{request.Id}/approve", null)).StatusCode);
        Assert.Equal(3, (await AnnualBalance()).UsedDays);
    }
    [Fact] public async Task RejectAfterApprovalReturnsConflict()
    {
        var request = await Submit(); await Login("manager@its.com", "Demo@123"); (await client.PutAsync($"/api/leaverequests/{request.Id}/approve", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/leaverequests/{request.Id}/reject", new { reason = "Coverage" })).StatusCode);
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public async Task PendingAndApprovedOverlapRejected(bool approve)
    {
        var request = await Submit();
        if (approve) { await Login("manager@its.com", "Demo@123"); (await client.PutAsync($"/api/leaverequests/{request.Id}/approve", null)).EnsureSuccessStatusCode(); }
        var balance = await AnnualBalance();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/leaverequests", new { leaveTypeId = balance.LeaveTypeId, startDate = new DateTime(2026, 11, 3), endDate = new DateTime(2026, 11, 4), reason = "Another vacation" })).StatusCode);
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public async Task RejectedAndCancelledDatesCanBeRequestedAgain(bool reject)
    {
        var request = await Submit();
        if (reject) { await Login("manager@its.com", "Demo@123"); (await client.PutAsJsonAsync($"/api/leaverequests/{request.Id}/reject", new { reason = "Coverage" })).EnsureSuccessStatusCode(); }
        else (await client.PutAsync($"/api/leaverequests/{request.Id}/cancel", null)).EnsureSuccessStatusCode();
        var another = await Submit(); Assert.NotEqual(request.Id, another.Id);
    }
    [Theory] [InlineData("2026-11-03", "2026-11-01")] [InlineData("2026-11-06", "2026-11-07")]
    public async Task InvalidDatesRejectedByApi(string start, string end)
    {
        var balance = await AnnualBalance();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/leaverequests", new { leaveTypeId = balance.LeaveTypeId, startDate = DateTime.Parse(start), endDate = DateTime.Parse(end), reason = "Vacation" })).StatusCode);
        Assert.Equal(0, (await AnnualBalance()).PendingDays);
    }
    [Fact] public async Task InsufficientBalanceRejectedByApi()
    {
        var balance = await AnnualBalance();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/leaverequests", new { leaveTypeId = balance.LeaveTypeId, startDate = new DateTime(2026, 11, 1), endDate = new DateTime(2026, 12, 31), reason = "Vacation" })).StatusCode);
    }
    [Fact] public async Task EmployeeCannotApproveOwnRequest()
    {
        var request = await Submit(); Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsync($"/api/leaverequests/{request.Id}/approve", null)).StatusCode);
    }
    [Fact] public async Task ManagerCannotApproveOutsideTeam()
    {
        var request = await Submit(); using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var employee = await db.Employees.SingleAsync(e => e.Email == "employee@its.com"); employee.ManagerId = null; await db.SaveChangesAsync();
        }
        await Login("manager@its.com", "Demo@123"); Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsync($"/api/leaverequests/{request.Id}/approve", null)).StatusCode);
        Assert.Equal(3, (await AnnualBalance()).PendingDays);
    }
    [Fact] public async Task BlankRejectionReasonRejected()
    {
        var request = await Submit(); await Login("manager@its.com", "Demo@123");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/leaverequests/{request.Id}/reject", new { reason = " " })).StatusCode);
        Assert.Equal(3, (await AnnualBalance()).PendingDays);
    }
    [Fact] public async Task LeaveSearchAndPaginationWork()
    {
        await Submit(reason: "Family trip"); await Submit("2026-11-08", "2026-11-10", "Medical appointment");
        var result = (await client.GetFromJsonAsync<PagedResultDto<LeaveRequestDto>>("/api/leaverequests/my?search=Family&pageSize=1&sortBy=startdate&sortOrder=desc"))!;
        Assert.Equal(1, result.TotalCount); Assert.Single(result.Items); Assert.Equal("Family trip", result.Items[0].Reason);
    }
    [Fact] public async Task TeamScopeCannotBeBypassedWithEmployeeFilter()
    {
        await Submit(); await Login("manager@its.com", "Demo@123");
        var result = (await client.GetFromJsonAsync<PagedResultDto<LeaveRequestDto>>("/api/leaverequests/team?employeeId=99999"))!; Assert.Empty(result.Items);
    }
    [Fact] public async Task SoftDeletePreservesLeaveAndAuditHistory()
    {
        var request = await Submit(); var profile = (await client.GetFromJsonAsync<EmployeeDto>("/api/employees/me"))!;
        await Login("admin@its.com", "Admin@123"); (await client.DeleteAsync($"/api/employees/{profile.EmployeeId}")).EnsureSuccessStatusCode();
        using var scope = host.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False((await db.Employees.SingleAsync(e => e.Id == profile.EmployeeId)).IsActive); Assert.True(await db.LeaveRequests.AnyAsync(r => r.Id == request.Id)); Assert.True(await db.AuditLogs.AnyAsync(a => a.Action == "Leave Submitted"));
    }
    [Fact] public async Task InactiveLeaveTypeCannotReceiveRequests()
    {
        var balance = await AnnualBalance(); await Login("admin@its.com", "Admin@123"); (await client.PutAsync($"/api/leavetypes/{balance.LeaveTypeId}/deactivate", null)).EnsureSuccessStatusCode();
        await Login("employee@its.com", "Demo@123"); Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/leaverequests", new { leaveTypeId = balance.LeaveTypeId, startDate = new DateTime(2026,11,1), endDate = new DateTime(2026,11,3), reason = "Vacation" })).StatusCode);
    }
    [Fact] public async Task NewLeaveTypeProvisionsExistingEmployees()
    {
        await Login("admin@its.com", "Admin@123"); (await client.PostAsJsonAsync("/api/leavetypes", new { name = "Personal Leave", allowedDays = 4 })).EnsureSuccessStatusCode();
        await Login("employee@its.com", "Demo@123"); var balances = (await client.GetFromJsonAsync<List<LeaveBalanceDto>>("/api/leaverequests/my-balance"))!; Assert.Equal(4, balances.Single(b => b.LeaveTypeName == "Personal Leave").AllocatedDays);
    }
    [Fact] public async Task LeaveTypeCannotReduceAllocationBelowCommitments()
    {
        await Submit(); var balance = await AnnualBalance(); await Login("admin@its.com", "Admin@123");
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/leavetypes/{balance.LeaveTypeId}", new { name = "Annual Leave", allowedDays = 2 })).StatusCode);
        Assert.Equal(21, (await AnnualBalance()).AllocatedDays);
    }
    [Fact] public async Task RoleChangeInvalidatesOldAccessToken()
    {
        var tokens = await Login("employee@its.com", "Demo@123"); var profile = (await client.GetFromJsonAsync<EmployeeDto>("/api/employees/me"))!;
        await Login("admin@its.com", "Admin@123"); (await client.PutAsync($"/api/employees/{profile.EmployeeId}/role?role=Manager", null)).EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/leaverequests/my")).StatusCode);
    }
    [Fact] public async Task ManagerCycleIsRejected()
    {
        await Login("admin@its.com", "Admin@123"); var employees = (await client.GetFromJsonAsync<EmployeePagedResultDto>("/api/employees"))!;
        var manager = employees.Employees.Single(e => e.Email == "manager@its.com"); var employee = employees.Employees.Single(e => e.Email == "employee@its.com");
        (await client.PutAsync($"/api/employees/{employee.EmployeeId}/role?role=Manager", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsync($"/api/employees/{manager.EmployeeId}/manager?managerId={employee.EmployeeId}", null)).StatusCode);
    }
    [Fact] public async Task AuditFailureRollsBackSubmissionAndReservation()
    {
        using var scope = host.Services.CreateScope(); var services = scope.ServiceProvider; var db = services.GetRequiredService<AppDbContext>();
        var employee = await db.Employees.SingleAsync(e => e.Email == "employee@its.com"); var type = await db.LeaveTypes.SingleAsync(t => t.Name == "Annual Leave");
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, employee.UserId!) }, "Test"));
        var service = new LeaveRequestService(services.GetRequiredService<ILeaveRequestRepository>(), services.GetRequiredService<IEmployeeRepository>(), new FailingAudit(), services.GetRequiredService<ITransactionRunner>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(principal, new() { LeaveTypeId = type.Id, StartDate = new(2026,11,1), EndDate = new(2026,11,3), Reason = "Vacation" }));
        db.ChangeTracker.Clear(); Assert.Equal(0, await db.LeaveRequests.CountAsync()); Assert.All(await db.LeaveBalances.ToListAsync(), b => Assert.Equal(0, b.PendingDays));
    }
    [Fact] public async Task AuditFailureRollsBackEmployeeAndIdentityAccount()
    {
        using var scope = host.Services.CreateScope(); var services = scope.ServiceProvider; var db = services.GetRequiredService<AppDbContext>(); var department = await db.Departments.SingleAsync();
        var service = new EmployeeService(services.GetRequiredService<IEmployeeRepository>(), new FailingAudit(), services.GetRequiredService<UserManager<ApplicationUser>>(), services.GetRequiredService<RoleManager<IdentityRole>>(), db, services.GetRequiredService<ITransactionRunner>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddAsync(new() { FullName = "Failed Employee", Email = "failed@its.com", Password = "Failed@123", DepartmentId = department.Id, JobTitle = "Developer", HireDate = DateTime.UtcNow.Date }));
        db.ChangeTracker.Clear(); Assert.False(await db.Employees.AnyAsync(e => e.Email == "failed@its.com")); Assert.False(await db.Users.AnyAsync(u => u.Email == "failed@its.com"));
    }
    [Fact] public async Task ManagerDashboardIncludesLastDayOfLeave()
    {
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var employee = await db.Employees.SingleAsync(e => e.Email == "employee@its.com"); var type = await db.LeaveTypes.SingleAsync(t => t.Name == "Annual Leave");
            db.LeaveRequests.Add(new() { EmployeeId = employee.Id, LeaveTypeId = type.Id, StartDate = DateTime.UtcNow.Date.AddDays(-3), EndDate = DateTime.UtcNow.Date, Days = 3, Reason = "Vacation", Status = LeaveRequestStatus.Approved, CreatedAt = DateTime.UtcNow }); await db.SaveChangesAsync();
        }
        await Login("manager@its.com", "Demo@123"); var dashboard = (await client.GetFromJsonAsync<ManagerDashboardDto>("/api/dashboard/manager"))!; Assert.Equal(1, dashboard.EmployeesCurrentlyAway);
    }
    private sealed class FailingAudit : IAuditLogService
    {
        public Task LogAsync(string action, string entityName, string entityId, string? details = null) => throw new InvalidOperationException("Audit unavailable");
        public Task<List<AuditLogDto>> GetAllAsync() => throw new NotSupportedException();
    }
}
