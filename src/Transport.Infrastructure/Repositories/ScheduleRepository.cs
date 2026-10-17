using Transport.Application.Abstractions;
using Transport.Domain.Entities;
using Transport.Domain.Enums;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Repositories;

internal sealed class ScheduleRepository(OracleConnectionFactory factory) : OracleRepository(factory), IScheduleRepository
{
    public Task AddAsync(Schedule schedule, CancellationToken ct = default) => ExecuteAsync(
        """
        INSERT INTO schedules (route_id, vehicle_id, driver_id, departure_time, arrival_time, ticket_price)
        VALUES (:p_route, :p_vehicle, :p_driver, :p_departure, :p_arrival, :p_price)
        """, Args(schedule), ct);

    public Task UpdateAsync(Schedule schedule, CancellationToken ct = default) => ExecuteAsync(
        """
        UPDATE schedules
           SET route_id = :p_route, vehicle_id = :p_vehicle, driver_id = :p_driver,
               departure_time = :p_departure, arrival_time = :p_arrival, ticket_price = :p_price
         WHERE schedule_id = :p_id
        """, Args(schedule), ct);

    public Task DeleteAsync(int scheduleId, CancellationToken ct = default) =>
        ExecuteAsync("DELETE FROM schedules WHERE schedule_id = :p_id", new { p_id = scheduleId }, ct);

    // Hủy chuyến phải qua CancelAsync (trigger TRG_SCHEDULES_CHECK_CANCEL chặn UPDATE trực tiếp khi còn vé)
    public Task SetStatusAsync(int scheduleId, ScheduleStatus status, CancellationToken ct = default) => ExecuteAsync(
        "UPDATE schedules SET status = :p_status WHERE schedule_id = :p_id",
        new { p_id = scheduleId, p_status = DbEnum.ToDb(status) }, ct);

    public Task CancelAsync(int scheduleId, string reason, CancellationToken ct = default) =>
        CallAsync("pkg_schedule.cancel_schedule", new { p_schedule_id = scheduleId, p_reason = reason }, ct);

    private static object Args(Schedule schedule) => new
    {
        p_id = schedule.Id,
        p_route = schedule.RouteId,
        p_vehicle = schedule.VehicleId,
        p_driver = schedule.DriverId,
        p_departure = schedule.DepartureTime,
        p_arrival = schedule.ArrivalTime,
        p_price = schedule.TicketPrice,
    };
}
