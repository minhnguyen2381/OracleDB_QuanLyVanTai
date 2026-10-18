using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Enums;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Repositories;

// Gửi yêu cầu bằng user REGISTRAR; duyệt/từ chối bằng tài khoản điều hành qua PKG_REGISTRATION
internal sealed class RegistrationRepository(OracleConnectionFactory factory) : OracleRepository(factory), IRegistrationRepository
{
    public Task SubmitAsync(string username, string fullName, string? phone, CancellationToken ct = default) =>
        CallAsRegistrarAsync("pkg_registration_public.submit_request",
            new { p_username = username, p_full_name = fullName, p_phone = phone }, ct);

    public Task<IReadOnlyList<RegistrationRequest>> GetAllAsync(CancellationToken ct = default) => QueryAsync<RegistrationRequest>(
        """
        SELECT request_id, username, full_name, phone, status, client_host,
               requested_at, reviewed_by, reviewed_at, reject_reason
          FROM registration_requests
         ORDER BY request_id DESC
        """, null, ct);

    public Task ApproveAsync(int requestId, string tempPassword, StaffRole role, CancellationToken ct = default) =>
        CallAsync("pkg_registration.approve_request",
            new { p_request_id = requestId, p_temp_password = tempPassword, p_role = DbEnum.RoleToDb(role) }, ct);

    public Task RejectAsync(int requestId, string reason, CancellationToken ct = default) =>
        CallAsync("pkg_registration.reject_request", new { p_request_id = requestId, p_reason = reason }, ct);
}
