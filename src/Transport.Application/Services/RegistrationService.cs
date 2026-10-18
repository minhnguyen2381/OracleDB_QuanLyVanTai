using Transport.Application.Abstractions;

namespace Transport.Application.Services;

// Đăng ký tài khoản từ màn hình đăng nhập (chưa đăng nhập)
public sealed class RegistrationService(IRegistrationRepository registrations)
{
    // Repository gọi bằng user TRANSPORT_REGISTRAR
    public Task SubmitAsync(string? username, string? fullName, string? phone, CancellationToken ct = default)
    {
        var user = username?.Trim();
        Rules.Username(user);
        Rules.Required(fullName);
        Rules.OptionalPhone(phone);
        return registrations.SubmitAsync(user!, fullName!.Trim(), Rules.TrimToNull(phone), ct);
    }
}
