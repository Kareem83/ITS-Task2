using ITS_Task.Models;
using ITS_Task.Business.DTOs;

namespace ITS_Task.DataAccess.Interfaces
{
    public interface ILeaveRequestRepository
    {
        Task<(List<LeaveRequest> Requests, int TotalCount)> QueryAsync(LeaveQueryDto options, int? scopeEmployeeId = null, int? scopeManagerId = null);
        Task<LeaveType?> GetLeaveTypeByIdAsync(int id);

        Task<LeaveBalance?> GetBalanceAsync(
            int employeeId,
            int leaveTypeId);

        Task<List<LeaveBalance>> GetBalancesByEmployeeIdAsync(
            int employeeId);

        Task<bool> HasOverlappingRequestAsync(
            int employeeId,
            DateTime startDate,
            DateTime endDate);

        Task<LeaveRequest> AddAsync(
            LeaveRequest request);

        Task UpdateBalanceAsync(
            LeaveBalance balance);

        Task<List<LeaveRequest>> GetPendingByManagerIdAsync(
            int managerId);

        Task<List<LeaveRequest>> GetByEmployeeIdAsync(
            int employeeId);

        Task<LeaveRequest?> GetByIdAsync(
            int id);

        Task<bool> CancelAsync(
            int requestId,
            int employeeId);

        Task<bool> ApproveAsync(
            int requestId,
            int managerId);

        Task<bool> RejectAsync(
            int requestId,
            int managerId,
            string reason);

        Task<List<LeaveRequest>> GetTeamRequestsAsync(
            int managerId);

        Task<(List<LeaveRequest> Requests, int TotalCount)> GetAllAsync(
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