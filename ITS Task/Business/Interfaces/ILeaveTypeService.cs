using ITS_Task.Business.DTOs;

namespace ITS_Task.Business.Interfaces
{
    public interface ILeaveTypeService
    {
        Task<List<LeaveTypeDto>> GetAllAsync();

        Task<LeaveTypeDto?> GetByIdAsync(
            int id);

        Task<LeaveTypeDto> CreateAsync(
            CreateLeaveTypeDto dto);

        Task<LeaveTypeDto?> UpdateAsync(
            int id,
            UpdateLeaveTypeDto dto);

        Task<bool> DeactivateAsync(
            int id);
    }
}