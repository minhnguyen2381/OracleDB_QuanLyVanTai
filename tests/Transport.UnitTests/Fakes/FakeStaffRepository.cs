using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Entities;
using Transport.Domain.Enums;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeStaffRepository : IStaffRepository
{
    public List<StaffMember> Staff { get; } = [];
    public List<string> Calls { get; } = [];
    public NewStaffRequest? LastCreate { get; private set; }

    public Task<IReadOnlyList<StaffMember>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<StaffMember>>(Staff.ToList());
    public Task CreateAsync(NewStaffRequest request, CancellationToken ct = default) { LastCreate = request; Calls.Add($"create:{request.Username}"); return Task.CompletedTask; }
    public Task UpdateInfoAsync(string username, string fullName, string? phone, CancellationToken ct = default) { Calls.Add($"update:{username}:{fullName}:{phone}"); return Task.CompletedTask; }
    public Task ChangeRoleAsync(string username, StaffRole role, CancellationToken ct = default) { Calls.Add($"role:{username}:{role}"); return Task.CompletedTask; }
    public Task ResetPasswordAsync(string username, string password, CancellationToken ct = default) { Calls.Add($"password:{username}"); return Task.CompletedTask; }
    public Task SetLockedAsync(string username, bool locked, CancellationToken ct = default) { Calls.Add($"lock:{username}:{locked}"); return Task.CompletedTask; }
}
