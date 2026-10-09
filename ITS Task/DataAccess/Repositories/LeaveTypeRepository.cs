using ITS_Task.DataAccess.Interfaces;
using ITS_Task.Models;
using Microsoft.EntityFrameworkCore;

namespace ITS_Task.DataAccess.Repositories
{
    public class LeaveTypeRepository : ILeaveTypeRepository
    {
        private readonly AppDbContext _context;

        public LeaveTypeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<LeaveType>> GetAllAsync()
        {
            return await _context.LeaveTypes
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<LeaveType?> GetByIdAsync(int id)
        {
            return await _context.LeaveTypes
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<LeaveType> AddAsync(
            LeaveType leaveType)
        {
            await _context.LeaveTypes.AddAsync(leaveType);
            await _context.SaveChangesAsync();

            return leaveType;
        }

        public async Task UpdateAsync(
            LeaveType leaveType)
        {
            _context.LeaveTypes.Update(leaveType);
            await _context.SaveChangesAsync();
        }
    }
}