using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Repositories;

internal sealed class StatisticsQueries(OracleConnectionFactory factory) : OracleRepository(factory), IStatisticsQueries
{
    public Task<IReadOnlyList<TripStatistic>> GetTripsAsync(TripSearchCriteria criteria, CancellationToken ct = default) =>
        QueryAsync<TripStatistic>(
            """
            SELECT schedule_id, route_id, vehicle_id, driver_id, route_name, origin, destination,
                   departure_time, arrival_time, ticket_price, schedule_status AS status, plate_number,
                   vehicle_type, driver_name, seat_count, seats_sold, seats_available, occupancy_pct,
                   tickets_cancelled, revenue
              FROM v_trip_statistics
             WHERE departure_time >= :p_from AND departure_time < :p_to
               AND (:p_route_id IS NULL OR route_id = :p_route_id)
               AND (:p_origin IS NULL OR UPPER(origin) LIKE '%' || UPPER(:p_origin) || '%')
               AND (:p_destination IS NULL OR UPPER(destination) LIKE '%' || UPPER(:p_destination) || '%')
               AND (:p_min_price IS NULL OR ticket_price >= :p_min_price)
               AND (:p_max_price IS NULL OR ticket_price <= :p_max_price)
               AND (:p_only_open = 0 OR (schedule_status = 'SCHEDULED' AND departure_time > SYSDATE))
             ORDER BY departure_time
            """,
            new
            {
                p_from = criteria.From,
                p_to = criteria.To,
                p_route_id = criteria.RouteId,
                p_origin = string.IsNullOrWhiteSpace(criteria.Origin) ? null : criteria.Origin.Trim(),
                p_destination = string.IsNullOrWhiteSpace(criteria.Destination) ? null : criteria.Destination.Trim(),
                p_min_price = criteria.MinPrice,
                p_max_price = criteria.MaxPrice,
                p_only_open = criteria.OnlyOpenForSale ? 1 : 0,
            }, ct);

    public Task<IReadOnlyList<DailyRevenue>> GetDailyRevenueAsync(DateTime from, DateTime to, CancellationToken ct = default) =>
        QueryAsync<DailyRevenue>(
            """
            SELECT TRUNC(departure_time) AS revenue_date, SUM(revenue) AS revenue, SUM(seats_sold) AS tickets
              FROM v_trip_statistics
             WHERE departure_time >= :p_from AND departure_time < :p_to AND schedule_status <> 'CANCELLED'
             GROUP BY TRUNC(departure_time)
             ORDER BY revenue_date
            """, new { p_from = from, p_to = to }, ct);
}
