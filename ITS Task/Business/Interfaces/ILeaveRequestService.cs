using ITS_Task.Business.DTOs;
using ITS_Task.Models;
using System.Security.Claims;

namespace ITS_Task.Business.Interfaces
{
    public interface ILeaveRequestService
    {
        Task<PagedResultDto<LeaveRequestDto>> QueryAsync(ClaimsPrincipal user, LeaveQueryDto query, string scope);
        Task<LeaveRequestDto> CreateAsync(
            ClaimsPrincipal user,
            CreateLeaveRequestDto dto);

        Task<List<LeaveRequestDto>> GetMyRequestsAsync(
            ClaimsPrincipal user);

        Task<bool> CancelAsync(
            ClaimsPrincipal user,
            int requestId);

        Task<LeaveRequestDto?> ApproveAsync(
            ClaimsPrincipal user,
            int requestId);

        Task<LeaveRequestDto?> RejectAsync(
            ClaimsPrincipal user,
            int requestId,
            string reason);

        Task<List<LeaveRequestDto>> GetPendingForManagerAsync(
            ClaimsPrincipal user);

        Task<List<LeaveRequestDto>> GetTeamRequestsAsync(
            ClaimsPrincipal user);

        Task<List<LeaveBalanceDto>> GetMyBalanceAsync(
            ClaimsPrincipal user);

        Task<(List<LeaveRequestDto> Requests, int TotalCount)> GetAllAsync(
            int? employeeId,
            int? departmentId,
            int? leaveTypeId,
            LeaveRequestStatus? status,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize,
            string sortBy,
            string sortOrder);
    }
}