using ITS_Task.DataAccess.Interfaces;
using ITS_Task.Models;
using Microsoft.EntityFrameworkCore;

namespace ITS_Task.DataAccess.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly AppDbContext _context;

        public AuditLogRepository(
            AppDbContext context)
        {
            _context = context;
        }

        public async Task<AuditLog> AddAsync(
            AuditLog auditLog)
        {
            await _context.AuditLogs.AddAsync(auditLog);

            await _context.SaveChangesAsync();

            return auditLog;
        }

        public async Task<List<AuditLog>> GetAllAsync()
        {
            return await _context.AuditLogs
                .OrderByDescending(x => x.Timestamp)
                .ToListAsync();
        }
    }
}