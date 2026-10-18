using Transport.Application.Abstractions;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Repositories;

// Đăng ký bằng user REGISTRAR qua PKG_REGISTRATION_PUBLIC
internal sealed class RegistrationRepository(OracleConnectionFactory factory) : OracleRepository(factory), IRegistrationRepository
{
    public Task SubmitAsync(string username, string fullName, string? phone, CancellationToken ct = default) =>
        CallAsRegistrarAsync("pkg_registration_public.submit_request",
            new { p_username = username, p_full_name = fullName, p_phone = phone }, ct);
}
