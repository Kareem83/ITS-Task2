using ITS_Task.Business.Exceptions;
using ITS_Task.Business.Services;
using ITS_Task.Models;
namespace ITS_Task.Tests;
[Trait("Category", "Unit")]
public class LeaveRulesTests
{
    [Theory]
    [InlineData("2026-09-06", "2026-09-10", 5)]
    [InlineData("2026-09-10", "2026-09-14", 3)]
    [InlineData("2026-09-06", "2026-09-06", 1)]
    [InlineData("2026-09-06T18:00:00", "2026-09-07T01:00:00", 2)]
    [InlineData("2026-09-01", "2026-09-30", 22)]
    public void WorkingDaysExcludeFridaySaturday(string start, string end, int expected)
        => Assert.Equal(expected, LeaveRules.CalculateWorkingDays(DateTime.Parse(start), DateTime.Parse(end)));
    [Theory]
    [InlineData("2026-09-10", "2026-09-09")]
    [InlineData("2026-09-11", "2026-09-12")]
    [InlineData("0001-01-01", "2026-09-06")]
    public void InvalidDatesAreRejected(string start, string end)
        => Assert.Equal(400, Assert.Throws<ApiException>(() => LeaveRules.CalculateWorkingDays(DateTime.Parse(start), DateTime.Parse(end))).StatusCode);
    [Theory]
    [InlineData("2026-09-12", "2026-09-18", true)]
    [InlineData("2026-09-15", "2026-09-18", true)]
    [InlineData("2026-09-16", "2026-09-18", false)]
    [InlineData("2026-09-01", "2026-09-09", false)]
    [InlineData("2026-09-10T18:00:00", "2026-09-10T20:00:00", true)]
    public void OverlapIsInclusiveAndDateBased(string start, string end, bool expected)
        => Assert.Equal(expected, LeaveRules.Overlaps(DateTime.Parse(start), DateTime.Parse(end), new(2026, 9, 10), new(2026, 9, 15)));
    [Fact] public void RemainingIncludesReservations() => Assert.Equal(11, new LeaveBalance { AllocatedDays = 21, UsedDays = 8, PendingDays = 2 }.RemainingDays);
    [Fact] public void ExcessLeaveIsRejectedWithoutMutation()
    {
        var balance = new LeaveBalance { AllocatedDays = 5 };
        Assert.Equal(409, Assert.Throws<ApiException>(() => LeaveRules.Reserve(balance, 7)).StatusCode);
        Assert.Equal(0, balance.PendingDays);
    }
    [Fact] public void ReservationDoesNotPermanentlyDeductUsed()
    {
        var balance = new LeaveBalance { AllocatedDays = 5 };
        LeaveRules.Reserve(balance, 3);
        Assert.Equal(3, balance.PendingDays); Assert.Equal(0, balance.UsedDays); Assert.Equal(2, balance.RemainingDays);
    }
    [Theory] [InlineData(0)] [InlineData(-1)]
    public void NonPositiveDaysRejected(int days) => Assert.Throws<ApiException>(() => LeaveRules.Reserve(new() { AllocatedDays = 5 }, days));
    [Theory] [InlineData(LeaveRequestStatus.Approved, 3)] [InlineData(LeaveRequestStatus.Rejected, 0)] [InlineData(LeaveRequestStatus.Cancelled, 0)]
    public void TransitionUpdatesCorrectCounters(LeaveRequestStatus target, int used)
    {
        var request = new LeaveRequest { Status = LeaveRequestStatus.Pending, Days = 3 };
        var balance = new LeaveBalance { AllocatedDays = 5, PendingDays = 3 };
        LeaveRules.Transition(request, balance, target);
        Assert.Equal(target, request.Status); Assert.Equal(0, balance.PendingDays); Assert.Equal(used, balance.UsedDays); Assert.Equal(5 - used, balance.RemainingDays);
    }
    [Theory] [InlineData(LeaveRequestStatus.Approved)] [InlineData(LeaveRequestStatus.Rejected)] [InlineData(LeaveRequestStatus.Cancelled)]
    public void DuplicateTransitionIsRejected(LeaveRequestStatus status)
    {
        var request = new LeaveRequest { Status = status, Days = 3 };
        var balance = new LeaveBalance { AllocatedDays = 5, PendingDays = 3 };
        Assert.Throws<ApiException>(() => LeaveRules.Transition(request, balance, LeaveRequestStatus.Approved)); Assert.Equal(3, balance.PendingDays);
    }
    [Fact] public void MissingReservationIsNotSilentlyClamped()
    {
        var balance = new LeaveBalance { AllocatedDays = 5, PendingDays = 1 };
        Assert.Throws<ApiException>(() => LeaveRules.Transition(new() { Days = 3, Status = LeaveRequestStatus.Pending }, balance, LeaveRequestStatus.Rejected));
        Assert.Equal(1, balance.PendingDays);
    }
    [Fact] public void PendingCannotTransitionToPending() => Assert.Throws<ApiException>(() => LeaveRules.Transition(new() { Days = 1, Status = LeaveRequestStatus.Pending }, new() { AllocatedDays = 5, PendingDays = 1 }, LeaveRequestStatus.Pending));
    [Fact] public void InvalidBalanceRejected() => Assert.Throws<ApiException>(() => LeaveRules.Reserve(new() { AllocatedDays = 5, UsedDays = 6 }, 1));
}
