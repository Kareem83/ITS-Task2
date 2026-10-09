using ITS_Task.Business.DTOs;

namespace ITS_Task.Business.Interfaces
{
    public interface IAuditLogService
    {
        Task LogAsync(
            string action,
            string entityName,
            string entityId,
            string? details = null);

        Task<List<AuditLogDto>> GetAllAsync();
        Task<PagedResultDto<AuditLogDto>> QueryAsync(AuditQueryDto query);
    }
}