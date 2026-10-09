using ITS_Task.Models;
namespace ITS_Task.DataAccess.Interfaces;
public interface IDepartmentRepository
{
    Task<List<Department>> GetAllAsync();
    Task<Department?> GetByIdAsync(int id);
    Task<bool> NameExistsAsync(string name, int? exceptId = null);
    Task<bool> HasEmployeesAsync(int id);
    Task<Department> AddAsync(Department department);
    Task SaveAsync();
    Task DeleteAsync(Department department);
}
