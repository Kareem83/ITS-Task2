using ITS_Task.Business.DTOs;
using ITS_Task.Business.Exceptions;
using ITS_Task.Business.Interfaces;
using ITS_Task.DataAccess;
using ITS_Task.DataAccess.Interfaces;
using ITS_Task.Models;
using Microsoft.EntityFrameworkCore;
namespace ITS_Task.Business.Services;
public class LeaveTypeService(ILeaveTypeRepository repository, AppDbContext context, ITransactionRunner transactions) : ILeaveTypeService
{
    private static LeaveTypeDto Map(LeaveType type) => new() { Id = type.Id, Name = type.Name, AllowedDays = type.AllowedDays, IsActive = type.IsActive, IsAnnual = type.IsAnnual };
    private static void Validate(string name, int days)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100 || days < 0 || days > 365) throw new ApiException(400, "Invalid leave type details");
    }
    public async Task<List<LeaveTypeDto>> GetAllAsync() => (await repository.GetAllAsync()).Select(Map).ToList();
    public async Task<LeaveTypeDto?> GetByIdAsync(int id) { var type = await repository.GetByIdAsync(id); return type == null ? null : Map(type); }
    public Task<LeaveTypeDto> CreateAsync(CreateLeaveTypeDto dto) => transactions.RunAsync(async () =>
    {
        Validate(dto.Name, dto.AllowedDays); var name = dto.Name.Trim();
        if (await context.LeaveTypes.AnyAsync(t => t.Name.ToUpper() == name.ToUpper())) throw new ApiException(409, "Leave type name already exists");
        var isAnnual = dto.IsAnnual || name.Equals("Annual Leave", StringComparison.OrdinalIgnoreCase);
        if (isAnnual && await context.LeaveTypes.AnyAsync(t => t.IsAnnual)) throw new ApiException(409, "An annual leave type already exists");
        var type = await repository.AddAsync(new LeaveType { Name = name, AllowedDays = dto.AllowedDays, IsAnnual = isAnnual });
        var employees = await context.Employees.Select(e => e.Id).ToListAsync();
        context.LeaveBalances.AddRange(employees.Select(id => new LeaveBalance { EmployeeId = id, LeaveTypeId = type.Id, AllocatedDays = type.AllowedDays }));
        await context.SaveChangesAsync(); return Map(type);
    });
    public Task<LeaveTypeDto?> UpdateAsync(int id, UpdateLeaveTypeDto dto) => transactions.RunAsync<LeaveTypeDto?>(async () =>
    {
        Validate(dto.Name, dto.AllowedDays); var name = dto.Name.Trim();
        var type = await repository.GetByIdAsync(id); if (type == null) return null;
        if (await context.LeaveTypes.AnyAsync(t => t.Id != id && t.Name.ToUpper() == name.ToUpper())) throw new ApiException(409, "Leave type name already exists");
        var isAnnual = dto.IsAnnual ?? type.IsAnnual;
        if (isAnnual && await context.LeaveTypes.AnyAsync(t => t.Id != id && t.IsAnnual)) throw new ApiException(409, "An annual leave type already exists");
        type.IsAnnual = isAnnual;
        var balances = await context.LeaveBalances.Where(b => b.LeaveTypeId == id).ToListAsync();
        if (balances.Any(b => b.UsedDays + b.PendingDays > dto.AllowedDays)) throw new ApiException(409, "Allocation cannot be lower than used and reserved days");
        type.Name = name; type.AllowedDays = dto.AllowedDays;
        foreach (var balance in balances) balance.AllocatedDays = dto.AllowedDays;
        await repository.UpdateAsync(type); return Map(type);
    });
    public Task<bool> DeactivateAsync(int id) => transactions.RunAsync(async () =>
    {
        var type = await repository.GetByIdAsync(id); if (type == null) return false;
        type.IsActive = false; await repository.UpdateAsync(type); return true;
    });
}
