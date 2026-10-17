using Transport.Application.Abstractions;
using Transport.Domain.Entities;

namespace Transport.UnitTests.Fakes;

internal class FakeCrudRepository<T>(Func<T, int> getId) : ICrudRepository<T>
{
    public List<T> Items { get; } = [];
    public List<T> Added { get; } = [];
    public List<T> Updated { get; } = [];
    public List<int> Deleted { get; } = [];
    public int GetAllCount { get; private set; }

    public Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
    {
        GetAllCount++;
        return Task.FromResult<IReadOnlyList<T>>(Items.ToList());
    }

    public Task AddAsync(T entity, CancellationToken ct = default)
    {
        Added.Add(entity);
        Items.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        Updated.Add(entity);
        var index = Items.FindIndex(x => getId(x) == getId(entity));
        if (index >= 0)
        {
            Items[index] = entity;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id, CancellationToken ct = default)
    {
        Deleted.Add(id);
        Items.RemoveAll(x => getId(x) == id);
        return Task.CompletedTask;
    }
}

internal sealed class FakeRouteRepository() : FakeCrudRepository<Route>(r => r.Id), IRouteRepository
{
}

internal sealed class FakeVehicleRepository() : FakeCrudRepository<Vehicle>(v => v.Id), IVehicleRepository
{
}

internal sealed class FakeDriverRepository() : FakeCrudRepository<Driver>(d => d.Id), IDriverRepository
{
}

internal sealed class FakePassengerRepository() : FakeCrudRepository<Passenger>(p => p.Id), IPassengerRepository
{
    public Task<Passenger?> FindByPhoneAsync(string phone, CancellationToken ct = default) =>
        Task.FromResult(Items.FirstOrDefault(p => p.Phone == phone));
}
