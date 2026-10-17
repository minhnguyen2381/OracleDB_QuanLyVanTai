using Transport.Application.Abstractions;
using Transport.Domain.Entities;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Repositories;

internal sealed class VehicleRepository(OracleConnectionFactory factory) : OracleRepository(factory), IVehicleRepository
{
    public Task<IReadOnlyList<Vehicle>> GetAllAsync(CancellationToken ct = default) => QueryAsync<Vehicle>(
        """
        SELECT vehicle_id AS id, plate_number, vehicle_type, seat_count, status
          FROM vehicles
         ORDER BY plate_number
        """, null, ct);

    public Task AddAsync(Vehicle vehicle, CancellationToken ct = default) => ExecuteAsync(
        """
        INSERT INTO vehicles (plate_number, vehicle_type, seat_count, status)
        VALUES (:p_plate, :p_type, :p_seats, :p_status)
        """, Args(vehicle), ct);

    public Task UpdateAsync(Vehicle vehicle, CancellationToken ct = default) => ExecuteAsync(
        """
        UPDATE vehicles
           SET plate_number = :p_plate, vehicle_type = :p_type, seat_count = :p_seats, status = :p_status
         WHERE vehicle_id = :p_id
        """, Args(vehicle), ct);

    public Task DeleteAsync(int id, CancellationToken ct = default) =>
        ExecuteAsync("DELETE FROM vehicles WHERE vehicle_id = :p_id", new { p_id = id }, ct);

    private static object Args(Vehicle vehicle) => new
    {
        p_id = vehicle.Id,
        p_plate = vehicle.PlateNumber,
        p_type = vehicle.VehicleType,
        p_seats = vehicle.SeatCount,
        p_status = DbEnum.ToDb(vehicle.Status),
    };
}
