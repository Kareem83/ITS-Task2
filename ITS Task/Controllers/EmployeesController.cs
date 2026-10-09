using ITS_Task.Business.DTOs;
using ITS_Task.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace ITS_Task.Controllers;
[ApiController]
[Route("api/[controller]")]
public class EmployeesController(IEmployeeService service) : ControllerBase
{
    [Authorize(Roles = "Employee,Manager")]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile() => Ok(await service.GetMyProfileAsync(User));
    [Authorize(Roles = "Administrator")]
    [HttpGet]
    [HttpGet("search")]
    public async Task<IActionResult> GetAll([FromQuery] EmployeeQueryDto query) => Ok(await service.QueryAsync(query));
    [Authorize(Roles = "Manager")]
    [HttpGet("my-team")]
    [HttpGet("my-team/search")]
    public async Task<IActionResult> GetMyTeam([FromQuery] EmployeeQueryDto query) => Ok(await service.QueryAsync(query, User));
    [Authorize(Roles = "Administrator")]
    [HttpPost("{id:int}/account")]
    public async Task<IActionResult> CreateAccount(int id, CreateEmployeeAccountDto dto)
    {
        var result = await service.CreateAccountAsync(id, dto);
        return result == null ? NotFound() : Ok(result);
    }
    [Authorize(Roles = "Administrator")]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await service.GetByIdAsync(id);
        return result == null ? NotFound() : Ok(result);
    }
    [Authorize(Roles = "Administrator")]
    [HttpPost]
    public async Task<IActionResult> Add(CreateEmployeeDto dto) => Ok(await service.AddAsync(dto));
    [Authorize(Roles = "Administrator")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateEmployeeDto dto)
    {
        var result = await service.UpdateAsync(id, dto);
        return result == null ? NotFound() : Ok(result);
    }
    [Authorize(Roles = "Administrator")]
    [HttpPut("{id:int}/deactivate")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id) => await service.DeactivateAsync(id) ? Ok() : NotFound();
    [Authorize(Roles = "Administrator")]
    [HttpPut("{id:int}/department")]
    public async Task<IActionResult> AssignDepartementToEmployee(int id, [FromQuery] int DepartmentId)
    {
        var result = await service.AssignDepartementToEmployee(id, DepartmentId);
        return result == null ? NotFound() : Ok(result);
    }
    [Authorize(Roles = "Administrator")]
    [HttpPut("{id:int}/manager")]
    public async Task<IActionResult> AssignManagerToEmployee(int id, [FromQuery] int managerId)
    {
        var result = await service.AssignManagerToEmployeeAsync(id, managerId);
        return result == null ? NotFound() : Ok(result);
    }
    [Authorize(Roles = "Administrator")]
    [HttpPut("{id:int}/role")]
    public async Task<IActionResult> AssignRoleToEmployee(int id, [FromQuery] string role)
        => await service.AssignRoleToEmployeeAsync(id, role) ? Ok("Role assigned successfully") : NotFound();
}
