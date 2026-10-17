namespace Transport.Application.Abstractions;

// Người đang đăng nhập
public interface IUserSession
{
    string Username { get; }
    string FullName { get; }
    bool IsOperator { get; }
    bool IsAuthenticated { get; }
}
