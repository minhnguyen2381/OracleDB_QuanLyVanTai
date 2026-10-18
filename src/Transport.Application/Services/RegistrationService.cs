using Transport.Application.Abstractions;

namespace Transport.Application.Services;

// Tự đăng ký tài khoản bán vé: Oracle tạo user ngay, không cần điều hành duyệt
public sealed class RegistrationService(IRegistrationRepository registrations)
{
    // Không cần đăng nhập; repository gọi bằng user TRANSPORT_REGISTRAR.
    // Mật khẩu phải đạt luật PROFILE_STAFF (báo sớm ở đây, Oracle kiểm tra lại bằng verify function)
    public Task RegisterAsync(string? username, string? password, string? fullName, string? phone, CancellationToken ct = default)
    {
        var user = username?.Trim();
        Rules.Username(user);
        Rules.Password(password, user);
        Rules.Required(fullName);
        Rules.OptionalPhone(phone);
        return registrations.RegisterAsync(user!, password!, fullName!.Trim(), Rules.TrimToNull(phone), ct);
    }
}
