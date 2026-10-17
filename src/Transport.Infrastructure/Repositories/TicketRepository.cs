using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Enums;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Repositories;

internal sealed class TicketRepository(OracleConnectionFactory factory) : OracleRepository(factory), ITicketRepository
{
    public async Task<IReadOnlySet<int>> GetBookedSeatsAsync(int scheduleId, CancellationToken ct = default)
    {
        var seats = await QueryAsync<int>(
            "SELECT seat_no FROM tickets WHERE schedule_id = :p_schedule_id AND status <> 'CANCELLED'",
            new { p_schedule_id = scheduleId }, ct);
        return seats.ToHashSet();
    }

    // Dùng OracleCommand trực tiếp vì cần đọc tham số OUT o_ticket_id
    public Task<long> BookAsync(BookingRequest request, CancellationToken ct = default) => RunAsync(async connection =>
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "pkg_ticket.book_ticket";
        command.CommandType = CommandType.StoredProcedure;
        command.BindByName = true;
        command.Parameters.Add("p_schedule_id", OracleDbType.Int32).Value = request.ScheduleId;
        command.Parameters.Add("p_full_name", OracleDbType.Varchar2).Value = request.FullName;
        command.Parameters.Add("p_phone", OracleDbType.Varchar2).Value = request.Phone;
        command.Parameters.Add("p_id_card", OracleDbType.Varchar2).Value = (object?)request.IdCard ?? DBNull.Value;
        command.Parameters.Add("p_seat_no", OracleDbType.Int32).Value = request.SeatNo;
        var ticketId = command.Parameters.Add("o_ticket_id", OracleDbType.Int64, ParameterDirection.Output);
        await command.ExecuteNonQueryAsync(ct);
        return ((OracleDecimal)ticketId.Value).ToInt64();
    }, ct);

    public Task PayAsync(long ticketId, PaymentMethod method, CancellationToken ct = default) =>
        CallAsync("pkg_ticket.pay_ticket", new { p_ticket_id = ticketId, p_method = DbEnum.ToDb(method) }, ct);

    public Task CancelAsync(long ticketId, string reason, CancellationToken ct = default) =>
        CallAsync("pkg_ticket.cancel_ticket", new { p_ticket_id = ticketId, p_reason = reason }, ct);

    public Task<IReadOnlyList<TicketListItem>> SearchAsync(TicketSearchCriteria criteria, CancellationToken ct = default) =>
        QueryAsync<TicketListItem>(
            """
            SELECT t.ticket_id, t.schedule_id, r.route_name, s.departure_time, v.plate_number, t.seat_no,
                   p.full_name AS passenger_name, p.phone, t.price, t.status, t.booked_at, t.sold_by, t.cancel_reason
              FROM tickets t
              JOIN schedules s  ON s.schedule_id  = t.schedule_id
              JOIN routes r     ON r.route_id     = s.route_id
              JOIN vehicles v   ON v.vehicle_id   = s.vehicle_id
              JOIN passengers p ON p.passenger_id = t.passenger_id
             WHERE (:p_keyword IS NULL
                    OR p.phone = :p_keyword
                    OR TO_CHAR(t.ticket_id) = :p_keyword
                    OR UPPER(p.full_name) LIKE '%' || UPPER(:p_keyword) || '%')
               AND (:p_status IS NULL OR t.status = :p_status)
               AND (:p_schedule_id IS NULL OR t.schedule_id = :p_schedule_id)
               AND (:p_from IS NULL OR s.departure_time >= :p_from)
               AND (:p_to IS NULL OR s.departure_time < :p_to)
             ORDER BY t.booked_at DESC
             FETCH FIRST 500 ROWS ONLY
            """,
            new
            {
                p_keyword = criteria.Keyword,
                p_status = criteria.Status is { } status ? DbEnum.ToDb(status) : null,
                p_schedule_id = criteria.ScheduleId,
                p_from = criteria.From,
                p_to = criteria.To,
            }, ct);

    public Task<TicketPrintModel?> GetPrintModelAsync(long ticketId, CancellationToken ct = default) =>
        QuerySingleOrDefaultAsync<TicketPrintModel>(
            """
            SELECT t.ticket_id, r.route_name, r.origin, r.destination, s.departure_time, s.arrival_time,
                   v.plate_number, v.vehicle_type, t.seat_no, p.full_name AS passenger_name, p.phone,
                   t.price, t.status, t.sold_by, t.booked_at
              FROM tickets t
              JOIN schedules s  ON s.schedule_id  = t.schedule_id
              JOIN routes r     ON r.route_id     = s.route_id
              JOIN vehicles v   ON v.vehicle_id   = s.vehicle_id
              JOIN passengers p ON p.passenger_id = t.passenger_id
             WHERE t.ticket_id = :p_ticket_id
            """, new { p_ticket_id = ticketId }, ct);
}
