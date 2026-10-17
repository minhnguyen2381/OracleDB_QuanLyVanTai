using Transport.Application.Abstractions;
using Transport.Domain.Entities;
using Transport.Domain.Enums;
using Transport.Domain.Errors;

namespace Transport.Application.Services;

// Lịch chạy: chỉ điều hành được thay đổi
public sealed class ScheduleService(IScheduleRepository schedules, IUserSession session)
{
    public async Task AddAsync(Schedule schedule, CancellationToken ct = default)
    {
        Rules.Operator(session);
        Validate(schedule);
        await schedules.AddAsync(schedule, ct);
    }

    public async Task UpdateAsync(Schedule schedule, CancellationToken ct = default)
    {
        Rules.Operator(session);
        Validate(schedule);
        await schedules.UpdateAsync(schedule, ct);
    }

    public async Task DeleteAsync(int scheduleId, CancellationToken ct = default)
    {
        Rules.Operator(session);
        await schedules.DeleteAsync(scheduleId, ct);
    }

    public async Task MarkRunningAsync(int scheduleId, CancellationToken ct = default)
    {
        Rules.Operator(session);
        await schedules.SetStatusAsync(scheduleId, ScheduleStatus.Running, ct);
    }

    public async Task MarkCompletedAsync(int scheduleId, CancellationToken ct = default)
    {
        Rules.Operator(session);
        await schedules.SetStatusAsync(scheduleId, ScheduleStatus.Completed, ct);
    }

    public async Task CancelAsync(int scheduleId, string reason, CancellationToken ct = default)
    {
        Rules.Operator(session);
        Rules.Required(reason);
        await schedules.CancelAsync(scheduleId, reason.Trim(), ct);
    }

    private static void Validate(Schedule schedule)
    {
        if (schedule.RouteId <= 0 || schedule.VehicleId <= 0 || schedule.DriverId <= 0)
        {
            throw new BusinessRuleException(BusinessErrorCode.RequiredField, "Route, vehicle and driver are required.");
        }

        if (schedule.ArrivalTime <= schedule.DepartureTime)
        {
            throw new BusinessRuleException(BusinessErrorCode.InvalidTimeRange, "Arrival time must be after departure time.");
        }

        if (schedule.TicketPrice <= 0)
        {
            throw new BusinessRuleException(BusinessErrorCode.InvalidPrice, "Ticket price must be greater than 0.");
        }
    }
}
