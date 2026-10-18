using Transport.Application.Abstractions;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeRegistrationRepository : IRegistrationRepository
{
    public List<string> Calls { get; } = [];

    public Task SubmitAsync(string username, string fullName, string? phone, CancellationToken ct = default)
    {
        Calls.Add($"submit:{username}:{fullName}:{phone}");
        return Task.CompletedTask;
    }
}
