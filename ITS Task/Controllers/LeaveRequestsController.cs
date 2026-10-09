using ITS_Task.Business.DTOs;
using ITS_Task.Business.Interfaces;
using ITS_Task.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITS_Task.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaveRequestsController : ControllerBase
    {
        private readonly ILeaveRequestService
            _leaveRequestService;

        public LeaveRequestsController(
            ILeaveRequestService leaveRequestService)
        {
            _leaveRequestService =
                leaveRequestService;
        }

        [Authorize(Roles = "Employee")]
        [HttpPost]
        public async Task<IActionResult> Create(
            CreateLeaveRequestDto dto)
        {
            var result =
                await _leaveRequestService
                    .CreateAsync(User, dto);

            return Ok(result);
        }

        [Authorize(Roles = "Employee")]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyRequests([FromQuery] LeaveQueryDto query) => Ok(await _leaveRequestService.QueryAsync(User, query, "mine"));


        [Authorize(Roles = "Employee")]
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> Cancel(
            int id)
        {
            var result =
                await _leaveRequestService
                    .CancelAsync(User, id);

            if (!result)
                return NotFound(
                    "Pending leave request not found");

            return Ok(
                "Leave request cancelled successfully");
        }

        [Authorize(Roles = "Employee")]
        [HttpGet("my-balance")]
        public async Task<IActionResult> GetMyBalance()
        {
            var result =
                await _leaveRequestService
                    .GetMyBalanceAsync(User);

            return Ok(result);
        }

        [Authorize(Roles = "Manager")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPending([FromQuery] LeaveQueryDto query) => Ok(await _leaveRequestService.QueryAsync(User, query, "pending"));


        [Authorize(Roles = "Manager")]
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> Approve(
            int id)
        {
            var result =
                await _leaveRequestService
                    .ApproveAsync(User, id);

            if (result == null)
                return NotFound(
                    "Leave request not found or does not belong to your team");

            return Ok(result);
        }

        [Authorize(Roles = "Manager")]
        [HttpPut("{id}/reject")]
        public async Task<IActionResult> Reject(
            int id,
            RejectLeaveRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result =
                await _leaveRequestService
                    .RejectAsync(
                        User,
                        id,
                        dto.Reason);

            if (result == null)
                return NotFound(
                    "Leave request not found or does not belong to your team");

            return Ok(result);
        }

        [Authorize(Roles = "Manager")]
        [HttpGet("team")]
        public async Task<IActionResult> GetTeamRequests([FromQuery] LeaveQueryDto query) => Ok(await _leaveRequestService.QueryAsync(User, query, "team"));


        [Authorize(Roles = "Administrator")]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] LeaveQueryDto query)
        {
            var result = await _leaveRequestService.QueryAsync(User, query, "all");
            return Ok(new { requests = result.Items, page = result.Page, pageSize = result.PageSize, totalCount = result.TotalCount, totalPages = result.TotalPages });
        }

    }
}