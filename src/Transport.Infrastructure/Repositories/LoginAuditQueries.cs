using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Repositories;

internal sealed class LoginAuditQueries(OracleConnectionFactory factory) : OracleRepository(factory), ILoginAuditQueries
{
    // Giới hạn 500 dòng mới nhất để lưới không quá nặng
    public Task<IReadOnlyList<LoginAuditEntry>> GetAsync(string? username, DateTime from, DateTime to, CancellationToken ct = default) =>
        QueryAsync<LoginAuditEntry>(
            """
            SELECT event_time, username, action_name, result, return_code, userhost AS user_host, object_name
              FROM v_login_audit
             WHERE event_time >= :p_from AND event_time < :p_to
               AND (:p_username IS NULL OR username = UPPER(:p_username))
             ORDER BY event_time DESC
             FETCH FIRST 500 ROWS ONLY
            """,
            new
            {
                p_from = from.Date,
                p_to = to.Date.AddDays(1),
                p_username = string.IsNullOrWhiteSpace(username) ? null : username.Trim(),
            }, ct);
}
