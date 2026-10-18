using Transport.Application.Abstractions;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Repositories;

// Tự đăng ký bằng user REGISTRAR qua PKG_REGISTRATION_PUBLIC
internal sealed class RegistrationRepository(OracleConnectionFactory factory) : OracleRepository(factory), IRegistrationRepository
{
    public Task RegisterAsync(string username, string password, string fullName, string? phone, CancellationToken ct = default) =>
        CallAsRegistrarAsync("pkg_registration_public.register_account",
            new { p_username = username, p_password = password, p_full_name = fullName, p_phone = phone }, ct);
}
