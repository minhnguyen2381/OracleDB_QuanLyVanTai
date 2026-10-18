namespace Transport.Application.Abstractions;

// Gọi PKG_REGISTRATION_PUBLIC bằng user TRANSPORT_REGISTRAR (chưa đăng nhập)
public interface IRegistrationRepository
{
    Task SubmitAsync(string username, string fullName, string? phone, CancellationToken ct = default);
}
