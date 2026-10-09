using ITS_Task.Business.DTOs;
using ITS_Task.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITS_Task.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaveTypesController : ControllerBase
    {
        private readonly ILeaveTypeService
            _leaveTypeService;

        public LeaveTypesController(
            ILeaveTypeService leaveTypeService)
        {
            _leaveTypeService =
                leaveTypeService;
        }

        [Authorize(Roles = "Administrator")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result =
                await _leaveTypeService.GetAllAsync();

            return Ok(result);
        }

        [Authorize(Roles = "Administrator")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(
            int id)
        {
            var result =
                await _leaveTypeService
                    .GetByIdAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost]
        public async Task<IActionResult> Create(
            CreateLeaveTypeDto dto)
        {
            var result =
                await _leaveTypeService
                    .CreateAsync(dto);

            return Ok(result);
        }

        [Authorize(Roles = "Administrator")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id,
            UpdateLeaveTypeDto dto)
        {
            var result =
                await _leaveTypeService
                    .UpdateAsync(id, dto);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [Authorize(Roles = "Administrator")]
        [HttpPut("{id}/deactivate")]
        public async Task<IActionResult> Deactivate(
            int id)
        {
            var result =
                await _leaveTypeService
                    .DeactivateAsync(id);

            if (!result)
                return NotFound();

            return Ok(
                "Leave type deactivated successfully");
        }
    }
}