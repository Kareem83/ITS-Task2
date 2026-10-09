using ITS_Task.DataAccess.Interfaces;
using ITS_Task.Models;
using ITS_Task.Business.DTOs;
using ITS_Task.Business.Exceptions;
using ITS_Task.Business.Services;
using Microsoft.EntityFrameworkCore;

namespace ITS_Task.DataAccess.Repositories
{
    public class LeaveRequestRepository : ILeaveRequestRepository
    {
        private readonly AppDbContext _context;

        public LeaveRequestRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<LeaveType?> GetLeaveTypeByIdAsync(int id)
        {
            return await _context.LeaveTypes
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<LeaveBalance?> GetBalanceAsync(
            int employeeId,
            int leaveTypeId)
        {
            return await _context.LeaveBalances
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == employeeId &&
                    x.LeaveTypeId == leaveTypeId);
        }

        public async Task<List<LeaveBalance>> GetBalancesByEmployeeIdAsync(
            int employeeId)
        {
            return await _context.LeaveBalances
                .Include(x => x.LeaveType)
                .Where(x => x.EmployeeId == employeeId)
                .ToListAsync();
        }

        public async Task<bool> HasOverlappingRequestAsync(
            int employeeId,
            DateTime startDate,
            DateTime endDate)
        {
            return await _context.LeaveRequests.AnyAsync(x =>
                x.EmployeeId == employeeId &&
                (x.Status == LeaveRequestStatus.Pending ||
                 x.Status == LeaveRequestStatus.Approved) &&
                startDate.Date <= x.EndDate.Date &&
                endDate.Date >= x.StartDate.Date);
        }

        public async Task<LeaveRequest> AddAsync(
            LeaveRequest request)
        {
            await _context.LeaveRequests.AddAsync(request);

            await _context.SaveChangesAsync();

            return request;
        }

        public async Task UpdateBalanceAsync(
            LeaveBalance balance)
        {
            _context.LeaveBalances.Update(balance);

            await _context.SaveChangesAsync();
        }

        public async Task<List<LeaveRequest>> GetPendingByManagerIdAsync(
            int managerId)
        {
            return await _context.LeaveRequests
                .Include(x => x.Employee)
                    .ThenInclude(x => x.Department)
                .Include(x => x.LeaveType)
                .Where(x =>
                    x.Employee.ManagerId == managerId &&
                    x.Status == LeaveRequestStatus.Pending)
                .OrderBy(x => x.StartDate)
                .ToListAsync();
        }

        public async Task<List<LeaveRequest>> GetByEmployeeIdAsync(
            int employeeId)
        {
            return await _context.LeaveRequests
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x => x.EmployeeId == employeeId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<LeaveRequest?> GetByIdAsync(int id)
        {
            return await _context.LeaveRequests
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public Task<bool> CancelAsync(int requestId, int employeeId) => ChangeAsync(requestId, employeeId, false, LeaveRequestStatus.Cancelled, null);
        public Task<bool> ApproveAsync(int requestId, int managerId) => ChangeAsync(requestId, managerId, true, LeaveRequestStatus.Approved, null);
        public Task<bool> RejectAsync(int requestId, int managerId, string reason) => ChangeAsync(requestId, managerId, true, LeaveRequestStatus.Rejected, reason);
        private async Task<bool> ChangeAsync(int requestId, int ownerId, bool manager, LeaveRequestStatus status, string? reason)
        {
            var request = await _context.LeaveRequests.Include(r => r.Employee).FirstOrDefaultAsync(r => r.Id == requestId);
            if (request == null || (manager ? request.Employee.ManagerId != ownerId : request.EmployeeId != ownerId)) return false;
            if (!request.Employee.IsActive) throw new ApiException(409, "Employee is inactive");
            if (status == LeaveRequestStatus.Rejected && (string.IsNullOrWhiteSpace(reason) || reason.Length > 500)) throw new ApiException(400, "A rejection reason of at most 500 characters is required");
            var balance = await GetBalanceAsync(request.EmployeeId, request.LeaveTypeId) ?? throw new ApiException(409, "Leave balance not found");
            LeaveRules.Transition(request, balance, status);
            request.RejectReason = status == LeaveRequestStatus.Rejected ? reason!.Trim() : null;
            await _context.SaveChangesAsync();
            return true;
        }








        public async Task<List<LeaveRequest>> GetTeamRequestsAsync(
            int managerId)
        {
            return await _context.LeaveRequests
                .Include(x => x.Employee)
                    .ThenInclude(x => x.Department)
                .Include(x => x.LeaveType)
                .Where(x =>
                    x.Employee.ManagerId == managerId)
                .OrderByDescending(x => x.StartDate)
                .ToListAsync();
        }

        public Task<(List<LeaveRequest> Requests, int TotalCount)> GetAllAsync(int? employeeId, int? departmentId, int? leaveTypeId, LeaveRequestStatus? status, DateTime? fromDate, DateTime? toDate, int page, int pageSize, string sortBy, string sortOrder)
            => QueryAsync(new() { EmployeeId = employeeId, DepartmentId = departmentId, LeaveTypeId = leaveTypeId, Status = status, FromDate = fromDate, ToDate = toDate, Page = page, PageSize = pageSize, SortBy = sortBy, SortOrder = sortOrder });
        public async Task<(List<LeaveRequest> Requests, int TotalCount)> QueryAsync(LeaveQueryDto options, int? scopeEmployeeId = null, int? scopeManagerId = null)
        {
            QueryRules.Validate(options, "id", "employee", "leavetype", "startdate", "status", "createdat");
            if (options.FromDate > options.ToDate) throw new ApiException(400, "Invalid date filter range");
            if (options.Status.HasValue && !Enum.IsDefined(options.Status.Value)) throw new ApiException(400, "Invalid leave status");
            var query = _context.LeaveRequests.Include(r => r.Employee).ThenInclude(e => e.Department).Include(r => r.LeaveType).AsNoTracking().AsQueryable();
            if (scopeEmployeeId.HasValue) query = query.Where(r => r.EmployeeId == scopeEmployeeId);
            if (scopeManagerId.HasValue) query = query.Where(r => r.Employee.ManagerId == scopeManagerId);
            if (options.EmployeeId.HasValue) query = query.Where(r => r.EmployeeId == options.EmployeeId);
            if (options.DepartmentId.HasValue) query = query.Where(r => r.Employee.DepartmentId == options.DepartmentId);
            if (options.LeaveTypeId.HasValue) query = query.Where(r => r.LeaveTypeId == options.LeaveTypeId);
            if (options.Status.HasValue) query = query.Where(r => r.Status == options.Status);
            if (options.FromDate.HasValue) query = query.Where(r => r.EndDate.Date >= options.FromDate.Value.Date);
            if (options.ToDate.HasValue) query = query.Where(r => r.StartDate.Date <= options.ToDate.Value.Date);
            if (!string.IsNullOrWhiteSpace(options.Search))
            {
                var term = options.Search.Trim();
                query = query.Where(r => r.Employee.FullName.Contains(term) || r.Employee.Email.Contains(term) || r.LeaveType.Name.Contains(term) || r.Reason.Contains(term));
            }
            var count = await query.CountAsync();
            var desc = options.SortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);
            IOrderedQueryable<LeaveRequest> ordered = options.SortBy.ToLowerInvariant() switch
            {
                "employee" => desc ? query.OrderByDescending(r => r.Employee.FullName) : query.OrderBy(r => r.Employee.FullName),
                "leavetype" => desc ? query.OrderByDescending(r => r.LeaveType.Name) : query.OrderBy(r => r.LeaveType.Name),
                "startdate" => desc ? query.OrderByDescending(r => r.StartDate) : query.OrderBy(r => r.StartDate),
                "status" => desc ? query.OrderByDescending(r => r.Status) : query.OrderBy(r => r.Status),
                "createdat" => desc ? query.OrderByDescending(r => r.CreatedAt) : query.OrderBy(r => r.CreatedAt),
                _ => desc ? query.OrderByDescending(r => r.Id) : query.OrderBy(r => r.Id)
            };
            return (await ordered.ThenBy(r => r.Id).Skip((options.Page - 1) * options.PageSize).Take(options.PageSize).ToListAsync(), count);
        }
    }
}
