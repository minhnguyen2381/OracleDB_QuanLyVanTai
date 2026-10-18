using Dapper;
using Oracle.ManagedDataAccess.Client;
using Transport.Application.Abstractions;
using Transport.Domain.Errors;

namespace Transport.Infrastructure.Persistence;

// Đăng nhập = mở kết nối Oracle bằng tài khoản nhân viên, đọc role đang bật và họ tên trong STAFF
internal sealed class OracleAuthService(OracleConnectionFactory factory, UserSession session) : IAuthService
{
    private const string OperatorRole = "ROLE_OPERATOR";
    private const string SellerRole = "ROLE_TICKET_SELLER";

    public Task LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var user = Normalize(username);
        return SignInAsync(user, password, () => factory.OpenAsync(user, password, ct), ct);
    }

    public Task LoginWithNewPasswordAsync(string username, string oldPassword, string newPassword, CancellationToken ct = default)
    {
        var user = Normalize(username);
        return SignInAsync(user, newPassword, () => factory.OpenWithNewPasswordAsync(user, oldPassword, newPassword, ct), ct);
    }

    public async Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct = default)
    {
        if (!session.IsAuthenticated)
        {
            throw new InvalidOperationException("No user is signed in.");
        }

        await LoginWithNewPasswordAsync(session.Username, currentPassword, newPassword, ct);
        // Kết nối trong pool vẫn mang mật khẩu cũ
        OracleConnection.ClearAllPools();
    }

    public void Logout()
    {
        session.Clear();
        // Bỏ các kết nối trong pool mang thông tin đăng nhập cũ
        OracleConnection.ClearAllPools();
    }

    // Sai mật khẩu, bị khóa, không phải nhân viên: cùng một lỗi để không lộ tài khoản nào tồn tại hay đang bị khóa
    internal static BusinessRuleException? MapLoginError(int number, string message) =>
        number is 1017 or 28000 ? InvalidCredentials() : OracleErrorMapper.Map(number, message);

    private async Task SignInAsync(string user, string password, Func<Task<OracleConnection>> open, CancellationToken ct)
    {
        try
        {
            await using var connection = await open();
            var roles = (await connection.QueryAsync<string>(
                    new CommandDefinition("SELECT role FROM session_roles", cancellationToken: ct)))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var fullName = await connection.ExecuteScalarAsync<string?>(new CommandDefinition(
                "SELECT full_name FROM staff WHERE username = :p_username", new { p_username = user }, cancellationToken: ct));

            // Chỉ nhân viên có trong STAFF và có một trong hai role mới vào được app (chặn cả TRANSPORT, REGISTRAR)
            var isOperator = roles.Contains(OperatorRole);
            if (fullName is null || (!isOperator && !roles.Contains(SellerRole)))
            {
                throw InvalidCredentials();
            }

            session.Start(user, password, fullName, isOperator);
        }
        catch (OracleException ex) when (MapLoginError(ex.Number, ex.Message) is { } mapped)
        {
            throw mapped;
        }
    }

    private static BusinessRuleException InvalidCredentials() =>
        new(BusinessErrorCode.InvalidCredentials, "Invalid username or password, or the account is locked.");

    private static string Normalize(string username) => username.Trim().ToUpperInvariant();
}
