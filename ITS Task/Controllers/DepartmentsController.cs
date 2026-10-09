using ITS_Task.Business.DTOs;
using ITS_Task.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace ITS_Task.Controllers;
[ApiController, Route("api/[controller]"), Authorize(Roles = "Administrator")]
public sealed class DepartmentsController(IDepartmentService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> GetAll() => Ok(await service.GetAllAsync());
    [HttpGet("{id:int}")] public async Task<IActionResult> GetById(int id) { var d = await service.GetByIdAsync(id); return d == null ? NotFound() : Ok(d); }
    [HttpPost] public async Task<IActionResult> Create(SaveDepartmentDto dto) => Ok(await service.CreateAsync(dto));
    [HttpPut("{id:int}")] public async Task<IActionResult> Update(int id, SaveDepartmentDto dto) { var d = await service.UpdateAsync(id, dto); return d == null ? NotFound() : Ok(d); }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id) => await service.DeleteAsync(id) ? Ok() : NotFound();
}
