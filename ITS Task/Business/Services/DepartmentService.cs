using ITS_Task.Business.DTOs;
using ITS_Task.Business.Exceptions;
using ITS_Task.Business.Interfaces;
using ITS_Task.DataAccess.Interfaces;
using ITS_Task.Models;
namespace ITS_Task.Business.Services;
public sealed class DepartmentService(IDepartmentRepository repository, ITransactionRunner transactions) : IDepartmentService
{
    private static DepartmentDto Map(Department d) => new() { Id = d.Id, Name = d.Name };
    private static string Name(SaveDepartmentDto dto) => !string.IsNullOrWhiteSpace(dto.Name) && dto.Name.Trim().Length <= 100 ? dto.Name.Trim() : throw new ApiException(400, "Invalid department name");
    public async Task<List<DepartmentDto>> GetAllAsync() => (await repository.GetAllAsync()).Select(Map).ToList();
    public async Task<DepartmentDto?> GetByIdAsync(int id) { var d = await repository.GetByIdAsync(id); return d == null ? null : Map(d); }
    public Task<DepartmentDto> CreateAsync(SaveDepartmentDto dto) => transactions.RunAsync(async () =>
    {
        var name = Name(dto);
        if (await repository.NameExistsAsync(name)) throw new ApiException(409, "Department name already exists");
        return Map(await repository.AddAsync(new Department { Name = name }));
    });
    public Task<DepartmentDto?> UpdateAsync(int id, SaveDepartmentDto dto) => transactions.RunAsync<DepartmentDto?>(async () =>
    {
        var name = Name(dto); var d = await repository.GetByIdAsync(id); if (d == null) return null;
        if (await repository.NameExistsAsync(name, id)) throw new ApiException(409, "Department name already exists");
        d.Name = name; await repository.SaveAsync(); return Map(d);
    });
    public Task<bool> DeleteAsync(int id) => transactions.RunAsync(async () =>
    {
        var d = await repository.GetByIdAsync(id); if (d == null) return false;
        if (await repository.HasEmployeesAsync(id)) throw new ApiException(409, "A department with employee history cannot be deleted");
        await repository.DeleteAsync(d); return true;
    });
}
