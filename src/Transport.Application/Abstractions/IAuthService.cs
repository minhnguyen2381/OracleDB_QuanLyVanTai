namespace Transport.Application.Abstractions;

public interface IAuthService
{
    // Đăng nhập bằng user Oracle; ném BusinessRuleException khi sai mật khẩu/bị khóa/không có quyền
    // (cùng mã InvalidCredentials) hoặc PasswordExpired khi phải đổi mật khẩu
    Task LoginAsync(string username, string password, CancellationToken ct = default);

    // Đăng nhập đồng thời đổi mật khẩu (dùng khi LoginAsync báo PasswordExpired)
    Task LoginWithNewPasswordAsync(string username, string oldPassword, string newPassword, CancellationToken ct = default);

    // Người đang đăng nhập tự đổi mật khẩu
    Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct = default);

    void Logout();
}
