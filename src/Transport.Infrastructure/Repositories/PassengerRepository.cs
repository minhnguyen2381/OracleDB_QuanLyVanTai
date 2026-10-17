using Transport.Application.Abstractions;
using Transport.Domain.Entities;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Repositories;

internal sealed class PassengerRepository(OracleConnectionFactory factory) : OracleRepository(factory), IPassengerRepository
{
    private const string SelectColumns = "SELECT passenger_id AS id, full_name, phone, id_card, email FROM passengers";

    public Task<IReadOnlyList<Passenger>> GetAllAsync(CancellationToken ct = default) =>
        QueryAsync<Passenger>($"{SelectColumns} ORDER BY full_name", null, ct);

    public Task<Passenger?> FindByPhoneAsync(string phone, CancellationToken ct = default) =>
        QuerySingleOrDefaultAsync<Passenger>($"{SelectColumns} WHERE phone = :p_phone", new { p_phone = phone }, ct);

    public Task AddAsync(Passenger passenger, CancellationToken ct = default) => ExecuteAsync(
        """
        INSERT INTO passengers (full_name, phone, id_card, email)
        VALUES (:p_name, :p_phone, :p_id_card, :p_email)
        """, Args(passenger), ct);

    public Task UpdateAsync(Passenger passenger, CancellationToken ct = default) => ExecuteAsync(
        """
        UPDATE passengers
           SET full_name = :p_name, phone = :p_phone, id_card = :p_id_card, email = :p_email
         WHERE passenger_id = :p_id
        """, Args(passenger), ct);

    public Task DeleteAsync(int id, CancellationToken ct = default) =>
        ExecuteAsync("DELETE FROM passengers WHERE passenger_id = :p_id", new { p_id = id }, ct);

    private static object Args(Passenger passenger) => new
    {
        p_id = passenger.Id,
        p_name = passenger.FullName,
        p_phone = passenger.Phone,
        p_id_card = passenger.IdCard,
        p_email = passenger.Email,
    };
}
