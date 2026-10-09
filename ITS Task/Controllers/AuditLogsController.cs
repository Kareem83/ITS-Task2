using ITS_Task.Business.Interfaces;
using ITS_Task.Business.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITS_Task.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuditLogsController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogsController(
            IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        [Authorize(Roles = "Administrator")]
        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged([FromQuery] AuditQueryDto query) => Ok(await _auditLogService.QueryAsync(query));

        [Authorize(Roles = "Administrator")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result =
                await _auditLogService.GetAllAsync();

            return Ok(result);
        }
    }
}