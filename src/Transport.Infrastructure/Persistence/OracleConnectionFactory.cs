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

    public Task<OracleConnection> OpenAsync(string username, string password, CancellationToken ct) =>
        OpenCoreAsync(Create(username, password, pooling: true), c => c.OpenAsync(ct), ct);

    // Đổi mật khẩu ngay lúc mở kết nối (dùng được cả khi mật khẩu đã hết hạn - ORA-28001).
    // Không qua pool: pool không mở được phiên bằng mật khẩu đã hết hạn.
    public Task<OracleConnection> OpenWithNewPasswordAsync(string username, string oldPassword, string newPassword,
        CancellationToken ct) =>
        OpenCoreAsync(Create(username, oldPassword, pooling: false),
            c => Task.Run(() => c.OpenWithNewPassword(newPassword), ct), ct);

    private OracleConnection Create(string username, string password, bool pooling) =>
        new(new OracleConnectionStringBuilder
        {
            DataSource = options.Value.DataSource,
            UserID = username,
            Password = password,
            Pooling = pooling,
        }.ConnectionString);

    private async Task<OracleConnection> OpenCoreAsync(OracleConnection connection, Func<OracleConnection, Task> open,
        CancellationToken ct)
    {
        try
        {
            var schema = options.Value.Schema;
            if (!SchemaPattern().IsMatch(schema))
            {
                throw new InvalidOperationException($"Invalid schema name '{schema}'.");
            }

            await open(connection);
            await using var command = connection.CreateCommand();
            // Nhờ vậy câu SQL trong app không cần tiền tố "transport."
            command.CommandText = $"ALTER SESSION SET CURRENT_SCHEMA = {schema}";
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
