namespace Transport.Application.Abstractions;

public interface IAuthService
{
    // Đăng nhập bằng user Oracle; ném BusinessRuleException khi sai mật khẩu/bị khóa/không có quyền
    Task LoginAsync(string username, string password, CancellationToken ct = default);
    void Logout();
}
