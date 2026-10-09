using ITS_Task.Business.Exceptions;
using ITS_Task.Models;
namespace ITS_Task.Business.Services;
public static class LeaveRules
{
    public static int CalculateWorkingDays(DateTime start, DateTime end)
    {
        start = start.Date;
        end = end.Date;
        if (start == DateTime.MinValue || end == DateTime.MinValue || end < start)
            throw new ApiException(400, "Invalid leave date range");
        var calendarDays = (end - start).Days + 1;
        var days = calendarDays / 7 * 5;
        for (var i = 0; i < calendarDays % 7; i++)
        {
            var date = start.AddDays(i);
            if (date.DayOfWeek != DayOfWeek.Friday && date.DayOfWeek != DayOfWeek.Saturday) days++;
        }
        if (days == 0) throw new ApiException(400, "Leave must contain a working day");
        return days;
    }
    public static bool Overlaps(DateTime start, DateTime end, DateTime otherStart, DateTime otherEnd)
        => start.Date <= otherEnd.Date && end.Date >= otherStart.Date;
    public static void Reserve(LeaveBalance balance, int days)
    {
        EnsureBalance(balance);
        if (days <= 0) throw new ApiException(400, "Invalid requested days");
        if (days > balance.RemainingDays) throw new ApiException(409, "Insufficient leave balance");
        balance.PendingDays += days;
    }
    public static void Transition(LeaveRequest request, LeaveBalance balance, LeaveRequestStatus target)
    {
        EnsureBalance(balance);
        if (request.Status != LeaveRequestStatus.Pending) throw new ApiException(409, "Only pending requests can change status");
        if (target is not (LeaveRequestStatus.Approved or LeaveRequestStatus.Rejected or LeaveRequestStatus.Cancelled))
            throw new ApiException(400, "Invalid leave transition");
        if (request.Days <= 0 || balance.PendingDays < request.Days)
            throw new ApiException(409, "Leave balance is inconsistent with the pending request");
        balance.PendingDays -= request.Days;
        if (target == LeaveRequestStatus.Approved) balance.UsedDays += request.Days;
        request.Status = target;
    }
    private static void EnsureBalance(LeaveBalance balance)
    {
        if (balance.AllocatedDays < 0 || balance.UsedDays < 0 || balance.PendingDays < 0 || balance.RemainingDays < 0)
            throw new ApiException(409, "Leave balance is inconsistent");
    }
}
