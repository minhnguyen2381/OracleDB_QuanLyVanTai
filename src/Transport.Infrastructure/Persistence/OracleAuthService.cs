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

    public async Task LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var user = username.Trim().ToUpperInvariant();
        try
        {
            await using var connection = await factory.OpenAsync(user, password, ct);
            var roles = (await connection.QueryAsync<string>(
                    new CommandDefinition("SELECT role FROM session_roles", cancellationToken: ct)))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var fullName = await connection.ExecuteScalarAsync<string?>(new CommandDefinition(
                "SELECT full_name FROM staff WHERE username = :p_username", new { p_username = user }, cancellationToken: ct));

            // Chỉ nhân viên có trong STAFF và có một trong hai role mới vào được app (chặn cả TRANSPORT)
            var isOperator = roles.Contains(OperatorRole);
            if (fullName is null || (!isOperator && !roles.Contains(SellerRole)))
            {
                throw new BusinessRuleException(BusinessErrorCode.InsufficientPrivileges, "This account cannot use the application.");
            }

            session.Start(user, password, fullName, isOperator);
        }
        catch (OracleException ex) when (OracleErrorMapper.Map(ex.Number, ex.Message) is { } mapped)
        {
            throw mapped;
        }
    }

    public void Logout()
    {
        session.Clear();
        // Bỏ các kết nối trong pool mang thông tin đăng nhập cũ
        OracleConnection.ClearAllPools();
    }
}
