using System.Data;
using Dapper;
using Oracle.ManagedDataAccess.Client;

namespace Transport.Infrastructure.Persistence;

// Lớp nền cho repository: mở kết nối, chạy lệnh, dịch lỗi Oracle
internal abstract class OracleRepository(OracleConnectionFactory factory)
{
    static OracleRepository()
    {
        // Cột route_name <-> thuộc tính RouteName
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    protected Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? args, CancellationToken ct) =>
        RunAsync<IReadOnlyList<T>>(
            async c => (await c.QueryAsync<T>(new CommandDefinition(sql, args, cancellationToken: ct))).AsList(), ct);

    protected Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? args, CancellationToken ct) =>
        RunAsync(c => c.QuerySingleOrDefaultAsync<T>(new CommandDefinition(sql, args, cancellationToken: ct)), ct);

    protected Task ExecuteAsync(string sql, object? args, CancellationToken ct) =>
        RunAsync(c => c.ExecuteAsync(new CommandDefinition(sql, args, cancellationToken: ct)), ct);

    // Gọi procedure/package, VD "pkg_ticket.pay_ticket"
    protected Task CallAsync(string procedure, object? args, CancellationToken ct) =>
        RunAsync(c => c.ExecuteAsync(new CommandDefinition(procedure, args,
            commandType: CommandType.StoredProcedure, cancellationToken: ct)), ct);

    protected async Task<T> RunAsync<T>(Func<OracleConnection, Task<T>> work, CancellationToken ct)
    {
        try
        {
            await using var connection = await factory.OpenAsync(ct);
            return await work(connection);
        }
        catch (OracleException ex) when (OracleErrorMapper.Map(ex.Number, ex.Message) is { } mapped)
        {
            throw mapped;
        }
    }
}
