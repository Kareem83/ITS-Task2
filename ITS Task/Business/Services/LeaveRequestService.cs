using ITS_Task.Business.DTOs;
using ITS_Task.Business.Interfaces;
using ITS_Task.DataAccess.Interfaces;
using ITS_Task.Models;
using ITS_Task.Business.Exceptions;
using System.Security.Claims;

namespace ITS_Task.Business.Services
{
    public class LeaveRequestService : ILeaveRequestService
    {
        private readonly ITransactionRunner _transactions;
        private readonly ILeaveRequestRepository
            _leaveRequestRepository;

        private readonly IEmployeeRepository
            _employeeRepository;

        private readonly IAuditLogService
            _auditLogService;

        public LeaveRequestService(
            ILeaveRequestRepository leaveRequestRepository,
            IEmployeeRepository employeeRepository,
            IAuditLogService auditLogService, ITransactionRunner transactions)
        {
            _leaveRequestRepository =
                leaveRequestRepository;

            _employeeRepository =
                employeeRepository;

            _auditLogService =
                auditLogService;
            _transactions = transactions;
        }

        public async Task<LeaveRequestDto> CreateAsync(
            ClaimsPrincipal user,
            CreateLeaveRequestDto dto)
        {
            return await _transactions.RunAsync<LeaveRequestDto>(async () =>
            {
            var employee =
                await GetCurrentEmployee(user);
            if (string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Length > 500) throw new ApiException(400, "A reason of at most 500 characters is required");

            if (dto.EndDate.Date < dto.StartDate.Date)
                throw new ApiException(400, 
                    "End date cannot be before start date");

            var leaveType =
                await _leaveRequestRepository
                    .GetLeaveTypeByIdAsync(
                        dto.LeaveTypeId);

            if (leaveType == null ||
                !leaveType.IsActive)
            {
                throw new ApiException(400, 
                    "Leave type is not available");
            }

            var days =
                CalculateWorkingDays(
                    dto.StartDate,
                    dto.EndDate);

            if (days <= 0)
                throw new ApiException(400, 
                    "Leave request must contain at least one working day");

            var overlap =
                await _leaveRequestRepository
                    .HasOverlappingRequestAsync(
                        employee.Id,
                        dto.StartDate,
                        dto.EndDate);

            if (overlap)
                throw new ApiException(409, 
                    "The requested dates overlap with an existing leave request.");

            var balance =
                await _leaveRequestRepository
                    .GetBalanceAsync(
                        employee.Id,
                        dto.LeaveTypeId);

            if (balance == null)
                throw new ApiException(409, 
                    "Leave balance not found");

            if (days > balance.RemainingDays)
                throw new ApiException(409, 
                    "Insufficient leave balance");

            LeaveRules.Reserve(balance, days);

            var request = new LeaveRequest
            {
                EmployeeId = employee.Id,
                LeaveTypeId = dto.LeaveTypeId,
                StartDate = dto.StartDate.Date,
                EndDate = dto.EndDate.Date,
                Days = days,
                Reason = dto.Reason.Trim(),
                Status = LeaveRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            var created =
                await _leaveRequestRepository
                    .AddAsync(request);

            await _leaveRequestRepository
                .UpdateBalanceAsync(balance);

            await _auditLogService.LogAsync(
                "Leave Submitted",
                "LeaveRequest",
                created.Id.ToString());

            return MapToDto(
                created,
                employee.FullName,
                leaveType.Name);
        
            });
        }

        public async Task<List<LeaveRequestDto>>
            GetMyRequestsAsync(
                ClaimsPrincipal user)
        {
            var employee =
                await GetCurrentEmployee(user);

            var requests =
                await _leaveRequestRepository
                    .GetByEmployeeIdAsync(
                        employee.Id);

            return requests.Select(x =>
                MapToDto(
                    x,
                    x.Employee.FullName,
                    x.LeaveType.Name))
                .ToList();
        }

        public async Task<bool> CancelAsync(
            ClaimsPrincipal user,
            int requestId)
        {
            return await _transactions.RunAsync<bool>(async () =>
            {
            var employee =
                await GetCurrentEmployee(user);

            var cancelled =
                await _leaveRequestRepository
                    .CancelAsync(
                        requestId,
                        employee.Id);

            if (cancelled)
            {
                await _auditLogService.LogAsync(
                    "Leave Cancelled",
                    "LeaveRequest",
                    requestId.ToString());
            }

            return cancelled;
        
            });
        }

        public async Task<List<LeaveRequestDto>>
            GetPendingForManagerAsync(
                ClaimsPrincipal user)
        {
            var manager =
                await GetCurrentEmployee(user);

            var requests =
                await _leaveRequestRepository
                    .GetPendingByManagerIdAsync(
                        manager.Id);

            return requests.Select(x =>
                MapToDto(
                    x,
                    x.Employee.FullName,
                    x.LeaveType.Name))
                .ToList();
        }

        public async Task<LeaveRequestDto?>
            ApproveAsync(
                ClaimsPrincipal user,
                int requestId)
        {
            return await _transactions.RunAsync<LeaveRequestDto?>(async () =>
            {
            var manager =
                await GetCurrentEmployee(user);

            var request =
                await _leaveRequestRepository
                    .GetByIdAsync(requestId);

            if (request == null)
                return null;

            if (request.Employee.ManagerId != manager.Id)
                return null;

            var approved =
                await _leaveRequestRepository
                    .ApproveAsync(
                        requestId,
                        manager.Id);

            if (!approved)
                return null;

            var updated =
                await _leaveRequestRepository
                    .GetByIdAsync(requestId);

            if (updated == null)
                return null;

            await _auditLogService.LogAsync(
                "Leave Approved",
                "LeaveRequest",
                requestId.ToString());

            return MapToDto(
                updated,
                updated.Employee.FullName,
                updated.LeaveType.Name);
        
            });
        }

        public async Task<LeaveRequestDto?>
            RejectAsync(
                ClaimsPrincipal user,
                int requestId,
                string reason)
        {
            return await _transactions.RunAsync<LeaveRequestDto?>(async () =>
            {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ApiException(400, 
                    "Reject reason is required");

            var manager =
                await GetCurrentEmployee(user);

            var request =
                await _leaveRequestRepository
                    .GetByIdAsync(requestId);

            if (request == null)
                return null;

            if (request.Employee.ManagerId != manager.Id)
                return null;

            var rejected =
                await _leaveRequestRepository
                    .RejectAsync(
                        requestId,
                        manager.Id,
                        reason);

            if (!rejected)
                return null;

            var updated =
                await _leaveRequestRepository
                    .GetByIdAsync(requestId);

            if (updated == null)
                return null;

            await _auditLogService.LogAsync(
                "Leave Rejected",
                "LeaveRequest",
                requestId.ToString(),
                reason);

            return MapToDto(
                updated,
                updated.Employee.FullName,
                updated.LeaveType.Name);
        
            });
        }

        public async Task<List<LeaveRequestDto>>
            GetTeamRequestsAsync(
                ClaimsPrincipal user)
        {
            var manager =
                await GetCurrentEmployee(user);

            var requests =
                await _leaveRequestRepository
                    .GetTeamRequestsAsync(
                        manager.Id);

            return requests.Select(x =>
                MapToDto(
                    x,
                    x.Employee.FullName,
                    x.LeaveType.Name))
                .ToList();
        }

        public async Task<List<LeaveBalanceDto>>
            GetMyBalanceAsync(
                ClaimsPrincipal user)
        {
            var employee =
                await GetCurrentEmployee(user);

            var balances =
                await _leaveRequestRepository
                    .GetBalancesByEmployeeIdAsync(
                        employee.Id);

            return balances.Select(x =>
                new LeaveBalanceDto
                {
                    Id = x.Id,
                    IsActive = x.LeaveType.IsActive,
                    LeaveTypeId = x.LeaveTypeId,
                    LeaveTypeName =
                        x.LeaveType.Name,
                    AllocatedDays =
                        x.AllocatedDays,
                    UsedDays =
                        x.UsedDays,
                    PendingDays =
                        x.PendingDays,
                    RemainingDays =
                        x.RemainingDays
                }).ToList();
        }

        public async Task<(List<LeaveRequestDto> Requests, int TotalCount)> GetAllAsync(int? employeeId, int? departmentId, int? leaveTypeId, LeaveRequestStatus? status, DateTime? fromDate, DateTime? toDate, int page, int pageSize, string sortBy, string sortOrder)
        {
            var result = await _leaveRequestRepository.GetAllAsync(employeeId, departmentId, leaveTypeId, status, fromDate, toDate, page, pageSize, sortBy, sortOrder);
            return (result.Requests.Select(r => MapToDto(r, r.Employee.FullName, r.LeaveType.Name)).ToList(), result.TotalCount);
        }


        public async Task<PagedResultDto<LeaveRequestDto>> QueryAsync(ClaimsPrincipal user, LeaveQueryDto query, string scope)
        {
            int? employeeId = null, managerId = null;
            if (scope != "all")
            {
                var employee = await GetCurrentEmployee(user);
                if (scope == "mine") employeeId = employee.Id;
                else if (scope is "team" or "pending") managerId = employee.Id;
                else throw new ApiException(400, "Invalid query scope");
                if (scope == "pending") query.Status = LeaveRequestStatus.Pending;
            }
            var result = await _leaveRequestRepository.QueryAsync(query, employeeId, managerId);
            return new() { Items = result.Requests.Select(r => MapToDto(r, r.Employee.FullName, r.LeaveType.Name)).ToList(), Page = query.Page, PageSize = query.PageSize, TotalCount = result.TotalCount };
        }

        private async Task<Employee>
            GetCurrentEmployee(
                ClaimsPrincipal user)
        {
            var userId =
                user.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException();

            var employee =
                await _employeeRepository
                    .GetByUserIdAsync(userId);

            if (employee == null)
                throw new ApiException(400, 
                    "Employee not found");

            if (!employee.IsActive) throw new ApiException(403, "Employee is inactive");
            return employee;
        }

        private int CalculateWorkingDays(DateTime startDate, DateTime endDate) => LeaveRules.CalculateWorkingDays(startDate, endDate);

        private LeaveRequestDto MapToDto(
            LeaveRequest request,
            string employeeName,
            string leaveTypeName)
        {
            return new LeaveRequestDto
            {
                Id = request.Id,
                EmployeeId = request.EmployeeId,
                EmployeeName = employeeName,
                LeaveTypeId = request.LeaveTypeId,
                LeaveTypeName = leaveTypeName,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Days = request.Days,
                Reason = request.Reason,
                Status = request.Status.ToString(),
                RejectReason = request.RejectReason,
                CreatedAt = request.CreatedAt
            };
        }
    }
}