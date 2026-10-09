using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ITS_Task.Business.DTOs;
using ITS_Task.Business.Interfaces;
using ITS_Task.DataAccess;
using ITS_Task.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace ITS_Task.Tests;
[Trait("Category", "Integration")]
public partial class ApiTests : IAsyncLifetime
{
    private readonly TestHost host = new();
    private HttpClient client = null!;
    public async Task InitializeAsync() { await host.InitializeAsync(); client = host.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false }); }
    public Task DisposeAsync() { client.Dispose(); host.Dispose(); return Task.CompletedTask; }
    private async Task<RefreshTokenDto> Login(string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password }); response.EnsureSuccessStatusCode();
        var tokens = (await response.Content.ReadFromJsonAsync<RefreshTokenDto>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return tokens;
    }
    [Theory] [InlineData("/api/employees")] [InlineData("/api/departments")] [InlineData("/api/auditlogs")] [InlineData("/api/leaverequests/my")]
    public async Task ProtectedEndpointsRequireAuthentication(string path) => Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(path)).StatusCode);
    [Fact] public async Task EmployeeCannotManageConfiguration()
    {
        await Login("employee@its.com", "Demo@123");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/departments")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/employees")).StatusCode);
    }
    [Fact] public async Task ProfileUsesCurrentUser()
    {
        await Login("employee@its.com", "Demo@123");
        var profile = await client.GetFromJsonAsync<EmployeeDto>("/api/employees/me"); Assert.Equal("employee@its.com", profile!.Email);
    }
    [Fact] public async Task InvalidPasswordRejected() => Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new { email = "employee@its.com", password = "Wrong@123" })).StatusCode);
    [Fact] public async Task RefreshRotatesAndOldTokenCannotBeReused()
    {
        var original = await Login("employee@its.com", "Demo@123");
        var response = await client.PostAsJsonAsync("/api/auth/refresh-token", new { refreshToken = original.RefreshToken }); response.EnsureSuccessStatusCode();
        var current = (await response.Content.ReadFromJsonAsync<RefreshTokenDto>())!; Assert.NotEqual(original.RefreshToken, current.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/refresh-token", new { refreshToken = original.RefreshToken })).StatusCode);
    }
    [Fact] public async Task ExpiredRefreshRejected()
    {
        var tokens = await Login("employee@its.com", "Demo@123");
        using var scope = host.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var account = await db.Users.SingleAsync(u => u.Email == "employee@its.com"); account.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(-1); await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/refresh-token", new { refreshToken = tokens.RefreshToken })).StatusCode);
    }
    [Fact] public async Task DeactivationRevokesAccessLoginAndRefresh()
    {
        var employeeTokens = await Login("employee@its.com", "Demo@123");
        var profile = (await client.GetFromJsonAsync<EmployeeDto>("/api/employees/me"))!;
        await Login("admin@its.com", "Admin@123"); (await client.PutAsync($"/api/employees/{profile.EmployeeId}/deactivate", null)).EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = new("Bearer", employeeTokens.AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/employees/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new { email = "employee@its.com", password = "Demo@123" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/refresh-token", new { refreshToken = employeeTokens.RefreshToken })).StatusCode);
    }
    [Fact] public async Task CreateEmployeeHasRoleAndBalances()
    {
        await Login("admin@its.com", "Admin@123");
        var departments = (await client.GetFromJsonAsync<List<DepartmentDto>>("/api/departments"))!;
        var response = await client.PostAsJsonAsync("/api/employees", new { fullName = "New Employee", email = "new@its.com", password = "New@123", departmentId = departments[0].Id, jobTitle = "Developer", hireDate = DateTime.UtcNow.Date }); response.EnsureSuccessStatusCode();
        await Login("new@its.com", "New@123"); (await client.GetAsync("/api/employees/me")).EnsureSuccessStatusCode();
        var balances = (await client.GetFromJsonAsync<List<LeaveBalanceDto>>("/api/leaverequests/my-balance"))!;
        Assert.Equal(2, balances.Count); Assert.All(balances, b => Assert.True(b.AllocatedDays > 0));
    }
    [Fact] public async Task DuplicateDepartmentReturnsConflict()
    {
        await Login("admin@its.com", "Admin@123");
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/departments", new { name = "engineering" })).StatusCode);
    }
    [Fact] public async Task DepartmentWithHistoryCannotBeDeleted()
    {
        await Login("admin@its.com", "Admin@123"); var d = (await client.GetFromJsonAsync<List<DepartmentDto>>("/api/departments"))![0];
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/departments/{d.Id}")).StatusCode);
    }
    [Theory] [InlineData("/api/employees?pageSize=0")] [InlineData("/api/leaverequests?pageSize=0")] [InlineData("/api/employees?sortBy=password")]
    public async Task InvalidListInputReturnsBadRequest(string path)
    {
        await Login("admin@its.com", "Admin@123"); Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(path)).StatusCode);
    }
    [Fact] public async Task EmployeeSearchReturnsPagedSortedResult()
    {
        await Login("admin@its.com", "Admin@123"); var result = (await client.GetFromJsonAsync<EmployeePagedResultDto>("/api/employees?search=Demo&pageSize=1&sortBy=name&sortOrder=desc"))!;
        Assert.Equal(2, result.TotalCount); Assert.Single(result.Employees); Assert.Equal("Demo Manager", result.Employees[0].FullName); Assert.Equal(2, result.TotalPages);
    }
    [Fact] public async Task InvalidRoleDoesNotRemoveExistingRole()
    {
        await Login("admin@its.com", "Admin@123"); var page = (await client.GetFromJsonAsync<EmployeePagedResultDto>("/api/employees?search=Demo%20Employee"))!;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsync($"/api/employees/{page.Employees[0].EmployeeId}/role?role=Missing", null)).StatusCode);
        await Login("employee@its.com", "Demo@123"); (await client.GetAsync("/api/employees/me")).EnsureSuccessStatusCode();
    }
    [Fact] public async Task UpdatedEmailIsUsedForLogin()
    {
        await Login("admin@its.com", "Admin@123"); var page = (await client.GetFromJsonAsync<EmployeePagedResultDto>("/api/employees?search=Demo%20Employee"))!; var e = page.Employees.Single();
        (await client.PutAsJsonAsync($"/api/employees/{e.EmployeeId}", new { fullName = e.FullName, email = "updated@its.com", departmentId = e.DepartmentId, jobTitle = e.JobTitle, hireDate = e.HireDate })).EnsureSuccessStatusCode();
        await Login("updated@its.com", "Demo@123");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new { email = "employee@its.com", password = "Demo@123" })).StatusCode);
    }
}
