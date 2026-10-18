using Transport.Application.Abstractions;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeRegistrationRepository : IRegistrationRepository
{
    public List<string> Calls { get; } = [];

    public Task RegisterAsync(string username, string password, string fullName, string? phone, CancellationToken ct = default)
    {
        Calls.Add($"register:{username}:{password}:{fullName}:{phone}");
        return Task.CompletedTask;
    }
}
