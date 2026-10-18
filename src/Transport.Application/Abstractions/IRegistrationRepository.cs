using Transport.Application.Models;
using Transport.Domain.Enums;

namespace Transport.Application.Abstractions;

// Gọi PKG_REGISTRATION_PUBLIC (chưa đăng nhập) và PKG_REGISTRATION (điều hành)
public interface IRegistrationRepository
{
    Task SubmitAsync(string username, string fullName, string? phone, CancellationToken ct = default);
    Task<IReadOnlyList<RegistrationRequest>> GetAllAsync(CancellationToken ct = default);
    Task ApproveAsync(int requestId, string tempPassword, StaffRole role, CancellationToken ct = default);
    Task RejectAsync(int requestId, string reason, CancellationToken ct = default);
}
