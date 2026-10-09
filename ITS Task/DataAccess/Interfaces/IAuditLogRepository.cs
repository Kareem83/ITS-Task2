using ITS_Task.Models;

namespace ITS_Task.DataAccess.Interfaces
{
    public interface IAuditLogRepository
    {
        Task<AuditLog> AddAsync(
            AuditLog auditLog);

        Task<List<AuditLog>> GetAllAsync();
    }
}