using System.ComponentModel.DataAnnotations;
using ITS_Task.Models;
namespace ITS_Task.Business.DTOs;
public class ListQueryDto
{
    [Range(1, 1000000)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 10;
    [StringLength(150)] public string? Search { get; set; }
    public string SortBy { get; set; } = "id";
    [RegularExpression("^(?i:asc|desc)$")] public string SortOrder { get; set; } = "asc";
}
public sealed class EmployeeQueryDto : ListQueryDto
{
    [Range(1, int.MaxValue)] public int? DepartmentId { get; set; }
    [Range(1, int.MaxValue)] public int? ManagerId { get; set; }
    public bool? IsActive { get; set; }
    public string? Role { get; set; }
}
public sealed class LeaveQueryDto : ListQueryDto
{
    [Range(1, int.MaxValue)] public int? EmployeeId { get; set; }
    [Range(1, int.MaxValue)] public int? DepartmentId { get; set; }
    [Range(1, int.MaxValue)] public int? LeaveTypeId { get; set; }
    [EnumDataType(typeof(LeaveRequestStatus))] public LeaveRequestStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
public sealed class PagedResultDto<T>
{
    public List<T> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed class AuditQueryDto : ListQueryDto
{
    public string? Action { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
