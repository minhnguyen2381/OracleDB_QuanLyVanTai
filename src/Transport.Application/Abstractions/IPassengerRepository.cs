using Transport.Domain.Entities;

namespace Transport.Application.Abstractions;

public interface IPassengerRepository : ICrudRepository<Passenger>
{
    Task<Passenger?> FindByPhoneAsync(string phone, CancellationToken ct = default);
}
