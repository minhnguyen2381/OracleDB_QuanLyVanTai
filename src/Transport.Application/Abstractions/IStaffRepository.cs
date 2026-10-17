using Transport.Application.Models;
using Transport.Domain.Entities;
using Transport.Domain.Enums;

namespace Transport.Application.Abstractions;

// Gọi PKG_STAFF
public interface IStaffRepository
{
    Task<IReadOnlyList<StaffMember>> GetAllAsync(CancellationToken ct = default);
    Task CreateAsync(NewStaffRequest request, CancellationToken ct = default);
    Task UpdateInfoAsync(string username, string fullName, string? phone, CancellationToken ct = default);
    Task ChangeRoleAsync(string username, StaffRole role, CancellationToken ct = default);
    Task ResetPasswordAsync(string username, string password, CancellationToken ct = default);
    Task SetLockedAsync(string username, bool locked, CancellationToken ct = default);
}
