using ITS_Task.Models;

namespace ITS_Task.DataAccess.Interfaces
{
    public interface ILeaveTypeRepository
    {
        Task<List<LeaveType>> GetAllAsync();

        Task<LeaveType?> GetByIdAsync(int id);

        Task<LeaveType> AddAsync(
            LeaveType leaveType);

        Task UpdateAsync(
            LeaveType leaveType);
    }
}