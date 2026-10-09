using ITS_Task.Business.DTOs;
using ITS_Task.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace ITS_Task.Controllers;
[ApiController, Route("api/[controller]")]
public sealed class DepartmentsController(IDepartmentService service) : ControllerBase
{
    [Authorize(Roles = "Manager"), HttpGet("team-options")]
    public async Task<IActionResult> GetTeamOptions() => Ok(await service.GetTeamOptionsAsync(User));
    [Authorize(Roles = "Administrator"), HttpGet] public async Task<IActionResult> GetAll() => Ok(await service.GetAllAsync());
    [Authorize(Roles = "Administrator"), HttpGet("{id:int}")] public async Task<IActionResult> GetById(int id) { var d = await service.GetByIdAsync(id); return d == null ? NotFound() : Ok(d); }
    [Authorize(Roles = "Administrator"), HttpPost] public async Task<IActionResult> Create(SaveDepartmentDto dto) => Ok(await service.CreateAsync(dto));
    [Authorize(Roles = "Administrator"), HttpPut("{id:int}")] public async Task<IActionResult> Update(int id, SaveDepartmentDto dto) { var d = await service.UpdateAsync(id, dto); return d == null ? NotFound() : Ok(d); }
    [Authorize(Roles = "Administrator"), HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id) => await service.DeleteAsync(id) ? Ok() : NotFound();
}
