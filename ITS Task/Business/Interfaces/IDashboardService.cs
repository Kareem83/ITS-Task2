using ITS_Task.Business.DTOs;
using System.Security.Claims;

namespace ITS_Task.Business.Interfaces
{
    public interface IDashboardService
    {
        Task<EmployeeDashboardDto> GetEmployeeDashboardAsync(
            ClaimsPrincipal user);

        Task<ManagerDashboardDto> GetManagerDashboardAsync(
            ClaimsPrincipal user);

        Task<AdminDashboardDto> GetAdminDashboardAsync();
    }
}