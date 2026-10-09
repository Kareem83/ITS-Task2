using ITS_Task.DataAccess.Interfaces;
using ITS_Task.Models;
using Microsoft.EntityFrameworkCore;
namespace ITS_Task.DataAccess.Repositories;
public sealed class DepartmentRepository(AppDbContext context) : IDepartmentRepository
{
    public Task<List<Department>> GetAllAsync() => context.Departments.AsNoTracking().OrderBy(d => d.Name).ThenBy(d => d.Id).ToListAsync();
    public Task<Department?> GetByIdAsync(int id) => context.Departments.FirstOrDefaultAsync(d => d.Id == id);
    public Task<bool> NameExistsAsync(string name, int? exceptId = null) => context.Departments.AnyAsync(d => d.Id != exceptId && d.Name.ToUpper() == name.ToUpper());
    public Task<bool> HasEmployeesAsync(int id) => context.Employees.AnyAsync(e => e.DepartmentId == id);
    public async Task<Department> AddAsync(Department department) { context.Departments.Add(department); await context.SaveChangesAsync(); return department; }
    public async Task SaveAsync() => await context.SaveChangesAsync();
    public async Task DeleteAsync(Department department) { context.Departments.Remove(department); await context.SaveChangesAsync(); }
}
