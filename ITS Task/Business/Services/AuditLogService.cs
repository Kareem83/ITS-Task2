using ITS_Task.Business.DTOs;
using ITS_Task.Business.Interfaces;
using ITS_Task.DataAccess.Interfaces;
using ITS_Task.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ITS_Task.Business.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLogService(
            IAuditLogRepository auditLogRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _auditLogRepository = auditLogRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(
            string action,
            string entityName,
            string entityId,
            string? details = null)
        {
            var user =
                _httpContextAccessor.HttpContext?.User;

            var userId =
                user?.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return;

            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Timestamp = DateTime.UtcNow,
                Details = details
            };

            await _auditLogRepository.AddAsync(auditLog);
        }

        public Task<PagedResultDto<AuditLogDto>> QueryAsync(AuditQueryDto query) => _auditLogRepository.QueryAsync(query);

        public async Task<List<AuditLogDto>> GetAllAsync()
        {
            var logs =
                await _auditLogRepository.GetAllAsync();

            return logs.Select(x => new AuditLogDto
            {
                Id = x.Id,
                UserId = x.UserId,
                Action = x.Action,
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                Timestamp = x.Timestamp,
                Details = x.Details
            }).ToList();
        }
    }
}