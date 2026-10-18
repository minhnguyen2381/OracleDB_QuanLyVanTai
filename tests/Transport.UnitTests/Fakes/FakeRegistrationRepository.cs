using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Enums;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeRegistrationRepository : IRegistrationRepository
{
    public List<RegistrationRequest> Requests { get; } = [];
    public List<string> Calls { get; } = [];

    public Task SubmitAsync(string username, string fullName, string? phone, CancellationToken ct = default) { Calls.Add($"submit:{username}:{fullName}:{phone}"); return Task.CompletedTask; }
    public Task<IReadOnlyList<RegistrationRequest>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<RegistrationRequest>>(Requests.ToList());
    public Task ApproveAsync(int requestId, string tempPassword, StaffRole role, CancellationToken ct = default) { Calls.Add($"approve:{requestId}:{role}"); return Task.CompletedTask; }
    public Task RejectAsync(int requestId, string reason, CancellationToken ct = default) { Calls.Add($"reject:{requestId}:{reason}"); return Task.CompletedTask; }
}
