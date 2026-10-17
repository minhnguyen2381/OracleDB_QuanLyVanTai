using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;

namespace Transport.Infrastructure.Persistence;

// Mở kết nối (lấy từ pool) bằng tài khoản Oracle của nhân viên đang đăng nhập
internal sealed partial class OracleConnectionFactory(IOptions<DatabaseOptions> options, UserSession session)
{
    public Task<OracleConnection> OpenAsync(CancellationToken ct) =>
        session.IsAuthenticated
            ? OpenAsync(session.Username, session.Password, ct)
            : throw new InvalidOperationException("No user is signed in.");

    public async Task<OracleConnection> OpenAsync(string username, string password, CancellationToken ct)
    {
        var settings = options.Value;
        if (!SchemaPattern().IsMatch(settings.Schema))
        {
            throw new InvalidOperationException($"Invalid schema name '{settings.Schema}'.");
        }

        var builder = new OracleConnectionStringBuilder
        {
            DataSource = settings.DataSource,
            UserID = username,
            Password = password,
        };
        var connection = new OracleConnection(builder.ConnectionString);
        try
        {
            await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            // Nhờ vậy câu SQL trong app không cần tiền tố "transport."
            command.CommandText = $"ALTER SESSION SET CURRENT_SCHEMA = {settings.Schema}";
            await command.ExecuteNonQueryAsync(ct);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_]{0,29}$")]
    private static partial Regex SchemaPattern();
}
