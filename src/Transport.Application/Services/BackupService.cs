using Transport.Application.Abstractions;
using Transport.Application.Models;

namespace Transport.Application.Services;

// Sao lưu dữ liệu vận hành – chỉ điều hành
public sealed class BackupService(IBackupRepository backups, IUserSession session)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        Rules.Operator(session);
        await backups.RunBackupAsync(ct);
    }

    public async Task<IReadOnlyList<BackupLog>> GetLogsAsync(CancellationToken ct = default)
    {
        Rules.Operator(session);
        return await backups.GetLogsAsync(ct);
    }

    public async Task<BackupJobStatus?> GetJobStatusAsync(CancellationToken ct = default)
    {
        Rules.Operator(session);
        return await backups.GetJobStatusAsync(ct);
    }
}
