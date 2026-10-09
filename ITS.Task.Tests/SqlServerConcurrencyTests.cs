using System.Security.Claims;
using ITS_Task.Business.DTOs;
using ITS_Task.Business.Exceptions;
using ITS_Task.Business.Interfaces;
using ITS_Task.Business.Services;
using ITS_Task.DataAccess;
using ITS_Task.DataAccess.Interfaces;
using ITS_Task.DataAccess.Repositories;
using ITS_Task.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace ITS_Task.Tests;
public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ITS_TEST_SQLSERVER"))) Skip = "Set ITS_TEST_SQLSERVER to run isolated SQL Server migration/concurrency tests";
    }
}
[Trait("Category", "SqlServer")]
public sealed class SqlServerConcurrencyTests
{
    private static async Task<ServiceProvider> CreateDatabase()
    {
        var builder = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ITS_TEST_SQLSERVER")) { InitialCatalog = "ITS_Automated_" + Guid.NewGuid().ToString("N") };
        var services = new ServiceCollection();
        services.AddLogging(); services.AddHttpContextAccessor();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["Seed:AdminPassword"] = "Admin@123", ["Seed:DemoPassword"] = "Demo@123" }).Build());
        services.AddDbContext<AppDbContext>(o => o.UseSqlServer(builder.ConnectionString, sql => sql.EnableRetryOnFailure()));
        services.AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<AppDbContext>();
        services.AddScoped<ITransactionRunner, TransactionRunner>(); services.AddScoped<SeedInitializer>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>(); services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>(); services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<ILeaveRequestService, LeaveRequestService>();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(); await scope.ServiceProvider.GetRequiredService<SeedInitializer>().InitializeAsync(true);
        return provider;
    }
    private static async Task<(int EmployeeId, string UserId, string ManagerUserId, int TypeId)> Ids(ServiceProvider provider)
    {
        using var scope = provider.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var employee = await db.Employees.SingleAsync(e => e.Email == "employee@its.com"); var manager = await db.Employees.SingleAsync(e => e.Email == "manager@its.com"); var type = await db.LeaveTypes.SingleAsync(t => t.Name == "Annual Leave");
        return (employee.Id, employee.UserId!, manager.UserId!, type.Id);
    }
    private static ClaimsPrincipal Principal(string id) => new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier,id) }, "Test"));
    private static async Task<int> Attempt(ServiceProvider provider, string userId, Func<ILeaveRequestService,ClaimsPrincipal,Task> action)
    {
        using var scope = provider.CreateScope();
        var principal = Principal(userId); scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext { User = principal };
        try { await action(scope.ServiceProvider.GetRequiredService<ILeaveRequestService>(), principal); return 200; }
        catch (ApiException exception) { return exception.StatusCode; }
    }
    [SqlServerFact] public async Task ConcurrentSubmissionsCannotOverlapOrDoubleReserve()
    {
        await using var provider = await CreateDatabase(); var ids = await Ids(provider);
        var dto = new CreateLeaveRequestDto { LeaveTypeId = ids.TypeId, StartDate = new(2026,11,1), EndDate = new(2026,11,3), Reason = "Vacation" };
        var results = await Task.WhenAll(Enumerable.Range(0,2).Select(_ => Task.Run(() => Attempt(provider, ids.UserId, async (service,user) => { await service.CreateAsync(user,dto); }))));
        Assert.Equal(1, results.Count(r => r == 200)); Assert.Equal(1, results.Count(r => r == 409));
        using var scope = provider.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.LeaveRequests.CountAsync()); var balance = await db.LeaveBalances.SingleAsync(b => b.EmployeeId == ids.EmployeeId && b.LeaveTypeId == ids.TypeId);
        Assert.Equal(3,balance.PendingDays); Assert.Equal(0,balance.UsedDays);
    }
    [SqlServerFact] public async Task SimultaneousApprovalsDeductOnce()
    {
        await using var provider = await CreateDatabase(); var ids = await Ids(provider); int requestId = 0;
        await Attempt(provider, ids.UserId, async (service,user) => requestId = (await service.CreateAsync(user,new() { LeaveTypeId = ids.TypeId, StartDate = new(2026,11,1), EndDate = new(2026,11,3), Reason = "Vacation" })).Id);
        var results = await Task.WhenAll(Enumerable.Range(0,2).Select(_ => Task.Run(() => Attempt(provider, ids.ManagerUserId, async (service,user) => { await service.ApproveAsync(user,requestId); }))));
        Assert.Equal(1,results.Count(r => r == 200)); Assert.Equal(1,results.Count(r => r == 409));
        using var scope = provider.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var balance = await db.LeaveBalances.SingleAsync(b => b.EmployeeId == ids.EmployeeId && b.LeaveTypeId == ids.TypeId);
        Assert.Equal(0,balance.PendingDays); Assert.Equal(3,balance.UsedDays); Assert.Equal(1,await db.AuditLogs.CountAsync(a => a.Action == "Leave Approved"));
    }
    [SqlServerFact] public async Task ConcurrentApprovalAndRejectionHaveOneWinner()
    {
        await using var provider = await CreateDatabase(); var ids = await Ids(provider); int requestId = 0;
        await Attempt(provider, ids.UserId, async (service,user) => requestId = (await service.CreateAsync(user,new() { LeaveTypeId = ids.TypeId, StartDate = new(2026,11,1), EndDate = new(2026,11,3), Reason = "Vacation" })).Id);
        var results = await Task.WhenAll(Task.Run(() => Attempt(provider,ids.ManagerUserId,async (service,user) => { await service.ApproveAsync(user,requestId); })),Task.Run(() => Attempt(provider,ids.ManagerUserId,async (service,user) => { await service.RejectAsync(user,requestId,"Coverage"); })));
        Assert.Equal(1,results.Count(r => r == 200)); Assert.Equal(1,results.Count(r => r == 409));
        using var scope = provider.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var request = await db.LeaveRequests.SingleAsync(); var balance = await db.LeaveBalances.SingleAsync(b => b.EmployeeId == ids.EmployeeId && b.LeaveTypeId == ids.TypeId);
        Assert.Equal(0,balance.PendingDays); Assert.Equal(request.Status == LeaveRequestStatus.Approved ? 3 : 0,balance.UsedDays);
        Assert.Equal(1,await db.AuditLogs.CountAsync(a => a.Action == "Leave Approved" || a.Action == "Leave Rejected"));
    }
}
