using Transport.Application.Models;

namespace Transport.Application.Abstractions;

public interface IBackupRepository
{
    Task RunBackupAsync(CancellationToken ct = default);
    Task<IReadOnlyList<BackupLog>> GetLogsAsync(CancellationToken ct = default);
    Task<BackupJobStatus?> GetJobStatusAsync(CancellationToken ct = default);
}
