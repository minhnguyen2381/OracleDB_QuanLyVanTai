using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Repositories;

internal sealed class BackupRepository(OracleConnectionFactory factory) : OracleRepository(factory), IBackupRepository
{
    public Task RunBackupAsync(CancellationToken ct = default) => CallAsync("backup_operational_data", null, ct);

    public Task<IReadOnlyList<BackupLog>> GetLogsAsync(CancellationToken ct = default) => QueryAsync<BackupLog>(
        """
        SELECT log_id, backup_at, performed_by, schedule_rows, ticket_rows, payment_rows
          FROM backup_logs
         ORDER BY log_id DESC
         FETCH FIRST 50 ROWS ONLY
        """, null, ct);

    // CAST bỏ múi giờ của TIMESTAMP WITH TIME ZONE để đọc ra DateTime
    public Task<BackupJobStatus?> GetJobStatusAsync(CancellationToken ct = default) => QuerySingleOrDefaultAsync<BackupJobStatus>(
        """
        SELECT job_name, enabled, state,
               CAST(last_start_date AS TIMESTAMP) AS last_start_date,
               CAST(next_run_date AS TIMESTAMP) AS next_run_date,
               failure_count
          FROM v_backup_job_status
        """, null, ct);
}
