using ITS_Task.DataAccess.Interfaces;
using ITS_Task.Models;
using ITS_Task.Business.DTOs;
using ITS_Task.Business.Services;
using ITS_Task.Business.Exceptions;
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

        public async Task<PagedResultDto<AuditLogDto>> QueryAsync(AuditQueryDto options)
        {
            QueryRules.Validate(options, "id", "timestamp", "action");
            if (options.FromDate > options.ToDate) throw new ApiException(400, "Invalid audit date range");
            var query = _context.AuditLogs.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(options.Action)) query = query.Where(a => a.Action == options.Action);
            if (options.FromDate.HasValue) query = query.Where(a => a.Timestamp >= options.FromDate.Value.Date);
            if (options.ToDate.HasValue) query = query.Where(a => a.Timestamp.Date <= options.ToDate.Value.Date);
            if (!string.IsNullOrWhiteSpace(options.Search))
            {
                var search = options.Search.Trim();
                query = query.Where(a => a.Action.Contains(search) || a.EntityName.Contains(search) || a.EntityId.Contains(search) || a.UserId.Contains(search) || (a.Details != null && a.Details.Contains(search)) || _context.Users.Any(u => u.Id == a.UserId && ((u.Email != null && u.Email.Contains(search)) || (u.Employee != null && u.Employee.FullName.Contains(search)))));
            }
            var count = await query.CountAsync();
            var desc = options.SortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);
            IOrderedQueryable<AuditLog> ordered = options.SortBy.ToLowerInvariant() switch
            {
                "id" => desc ? query.OrderByDescending(a => a.Id) : query.OrderBy(a => a.Id),
                "action" => desc ? query.OrderByDescending(a => a.Action) : query.OrderBy(a => a.Action),
                _ => desc ? query.OrderByDescending(a => a.Timestamp) : query.OrderBy(a => a.Timestamp)
            };
            var items = await ordered.ThenBy(a => a.Id).Skip((options.Page - 1) * options.PageSize).Take(options.PageSize).Select(a => new AuditLogDto
            {
                Id = a.Id, UserId = a.UserId, Action = a.Action, EntityName = a.EntityName, EntityId = a.EntityId, Timestamp = a.Timestamp, Details = a.Details,
                UserName = _context.Users.Where(u => u.Id == a.UserId).Select(u => u.Employee != null ? u.Employee.FullName : u.Email).FirstOrDefault()
            }).ToListAsync();
            return new() { Items = items, Page = options.Page, PageSize = options.PageSize, TotalCount = count };
        }

        public async Task<List<AuditLog>> GetAllAsync()
        {
            return await _context.AuditLogs
                .OrderByDescending(x => x.Timestamp)
                .ToListAsync();
        }
    }
}