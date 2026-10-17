using Transport.Application.Abstractions;
using Transport.Domain.Entities;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Repositories;

internal sealed class RouteRepository(OracleConnectionFactory factory) : OracleRepository(factory), IRouteRepository
{
    public Task<IReadOnlyList<Route>> GetAllAsync(CancellationToken ct = default) => QueryAsync<Route>(
        """
        SELECT route_id AS id, route_name AS name, origin, destination, distance_km, duration_min, base_price
          FROM routes
         ORDER BY route_name
        """, null, ct);

    public Task AddAsync(Route route, CancellationToken ct = default) => ExecuteAsync(
        """
        INSERT INTO routes (route_name, origin, destination, distance_km, duration_min, base_price)
        VALUES (:p_name, :p_origin, :p_destination, :p_distance, :p_duration, :p_price)
        """, Args(route), ct);

    public Task UpdateAsync(Route route, CancellationToken ct = default) => ExecuteAsync(
        """
        UPDATE routes
           SET route_name = :p_name, origin = :p_origin, destination = :p_destination,
               distance_km = :p_distance, duration_min = :p_duration, base_price = :p_price
         WHERE route_id = :p_id
        """, Args(route), ct);

    public Task DeleteAsync(int id, CancellationToken ct = default) =>
        ExecuteAsync("DELETE FROM routes WHERE route_id = :p_id", new { p_id = id }, ct);

    private static object Args(Route route) => new
    {
        p_id = route.Id,
        p_name = route.Name,
        p_origin = route.Origin,
        p_destination = route.Destination,
        p_distance = route.DistanceKm,
        p_duration = route.DurationMin,
        p_price = route.BasePrice,
    };
}
