using ITS_Task.Business.DTOs;
using ITS_Task.Business.Interfaces;
using ITS_Task.DataAccess;
using ITS_Task.DataAccess.Interfaces;
using ITS_Task.Models;
using ITS_Task.Business.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ITS_Task.Business.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmployeeRepository _employeeRepository;

        public DashboardService(
            AppDbContext context,
            IEmployeeRepository employeeRepository, IConfiguration configuration)
        {
            _context = context;
            _employeeRepository = employeeRepository;
            _configuration = configuration;
        }

        public async Task<EmployeeDashboardDto>
            GetEmployeeDashboardAsync(
                ClaimsPrincipal user)
        {
            var userId = user.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException();

            var employee =
                await _employeeRepository
                    .GetByUserIdAsync(userId);

            if (employee == null)
                throw new ApiException(404, 
                    "Employee not found");

            if (!employee.IsActive) throw new ApiException(403, "Employee is inactive");
            var currentYear = DateTime.UtcNow.Year;
            var annualId = _configuration.GetValue<int?>("LeaveManagement:AnnualLeaveTypeId");

            var balance =
                await _context.LeaveBalances
                    .Include(x => x.LeaveType)
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId == employee.Id &&
                        (annualId.HasValue ? x.LeaveTypeId == annualId.Value : x.LeaveType.IsAnnual));

            var pendingRequests =
                await _context.LeaveRequests
                    .CountAsync(x =>
                        x.EmployeeId == employee.Id &&
                        x.Status ==
                        LeaveRequestStatus.Pending);

            var approvedThisYear =
                await _context.LeaveRequests
                    .CountAsync(x =>
                        x.EmployeeId == employee.Id &&
                        x.Status ==
                        LeaveRequestStatus.Approved &&
                        x.StartDate.Year == currentYear);

            var rejectedRequests =
                await _context.LeaveRequests
                    .CountAsync(x =>
                        x.EmployeeId == employee.Id &&
                        x.Status ==
                        LeaveRequestStatus.Rejected);

            return new EmployeeDashboardDto
            {
                AnnualLeaveBalance =
                    balance?.RemainingDays ?? 0,

                PendingRequests =
                    pendingRequests,

                ApprovedThisYear =
                    approvedThisYear,

                RejectedRequests =
                    rejectedRequests
            };
        }

        public async Task<ManagerDashboardDto>
            GetManagerDashboardAsync(
                ClaimsPrincipal user)
        {
            var userId = user.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException();

            var manager =
                await _employeeRepository
                    .GetByUserIdAsync(userId);

            if (manager == null)
                throw new ApiException(404, 
                    "Manager not found");

            if (!manager.IsActive) throw new ApiException(403, "Manager is inactive");
            var teamMembers =
                await _context.Employees
                    .CountAsync(x =>
                        x.ManagerId == manager.Id);

            var pendingRequests =
                await _context.LeaveRequests
                    .CountAsync(x =>
                        x.Employee.ManagerId == manager.Id &&
                        x.Status ==
                        LeaveRequestStatus.Pending);

            var now = DateTime.UtcNow;

            var approvedThisMonth =
                await _context.LeaveRequests
                    .CountAsync(x =>
                        x.Employee.ManagerId == manager.Id &&
                        x.Status ==
                        LeaveRequestStatus.Approved &&
                        x.StartDate.Year == now.Year &&
                        x.StartDate.Month == now.Month);

            var employeesCurrentlyAway =
                await _context.Employees
                    .CountAsync(x =>
                        x.ManagerId == manager.Id &&
                        _context.LeaveRequests.Any(l =>
                            l.EmployeeId == x.Id &&
                            l.Status ==
                            LeaveRequestStatus.Approved &&
                            l.StartDate.Date <= now.Date &&
                            l.EndDate.Date >= now.Date));

            return new ManagerDashboardDto
            {
                TeamMembers =
                    teamMembers,

                PendingRequests =
                    pendingRequests,

                ApprovedThisMonth =
                    approvedThisMonth,

                EmployeesCurrentlyAway =
                    employeesCurrentlyAway
            };
        }

        public async Task<AdminDashboardDto>
            GetAdminDashboardAsync()
        {
            var now = DateTime.UtcNow;

            var totalEmployees =
                await _context.Employees
                    .CountAsync();

            var activeEmployees =
                await _context.Employees
                    .CountAsync(x =>
                        x.IsActive);

            var departments =
                await _context.Departments
                    .CountAsync();

            var pendingLeaveRequests =
                await _context.LeaveRequests
                    .CountAsync(x =>
                        x.Status ==
                        LeaveRequestStatus.Pending);

            var approvedThisMonth =
                await _context.LeaveRequests
                    .CountAsync(x =>
                        x.Status ==
                        LeaveRequestStatus.Approved &&
                        x.StartDate.Year == now.Year &&
                        x.StartDate.Month == now.Month);

            return new AdminDashboardDto
            {
                TotalEmployees =
                    totalEmployees,

                ActiveEmployees =
                    activeEmployees,

                Departments =
                    departments,

                PendingLeaveRequests =
                    pendingLeaveRequests,

                ApprovedThisMonth =
                    approvedThisMonth
            };
        }
    }
}