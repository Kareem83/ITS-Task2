using ITS_Task.Models;
using ITS_Task.Business.DTOs;

namespace ITS_Task.DataAccess.Interfaces
{
    public interface IAuditLogRepository
    {
        Task<AuditLog> AddAsync(
            AuditLog auditLog);

        Task<List<AuditLog>> GetAllAsync();
        Task<PagedResultDto<AuditLogDto>> QueryAsync(AuditQueryDto query);
    }
}