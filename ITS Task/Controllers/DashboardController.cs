using ITS_Task.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITS_Task.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService
            _dashboardService;

        public DashboardController(
            IDashboardService dashboardService)
        {
            _dashboardService =
                dashboardService;
        }

        [Authorize(Roles = "Employee")]
        [HttpGet("employee")]
        public async Task<IActionResult>
            GetEmployeeDashboard()
        {
            var result =
                await _dashboardService
                    .GetEmployeeDashboardAsync(User);

            return Ok(result);
        }

        [Authorize(Roles = "Manager")]
        [HttpGet("manager")]
        public async Task<IActionResult>
            GetManagerDashboard()
        {
            var result =
                await _dashboardService
                    .GetManagerDashboardAsync(User);

            return Ok(result);
        }

        [Authorize(Roles = "Administrator")]
        [HttpGet("admin")]
        public async Task<IActionResult>
            GetAdminDashboard()
        {
            var result =
                await _dashboardService
                    .GetAdminDashboardAsync();

            return Ok(result);
        }
    }
}