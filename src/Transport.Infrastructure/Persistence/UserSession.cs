using Transport.Application.Abstractions;

namespace Transport.Infrastructure.Persistence;

// Thông tin đăng nhập của phiên; mật khẩu chỉ giữ trong bộ nhớ để mở kết nối từ pool
internal sealed class UserSession : IUserSession
{
    public string Username { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public bool IsOperator { get; private set; }
    public bool IsAuthenticated => Username.Length > 0;

    internal string Password { get; private set; } = string.Empty;

    internal void Start(string username, string password, string fullName, bool isOperator)
    {
        Username = username;
        Password = password;
        FullName = fullName;
        IsOperator = isOperator;
    }

    internal void Clear() => Start(string.Empty, string.Empty, string.Empty, false);
}
