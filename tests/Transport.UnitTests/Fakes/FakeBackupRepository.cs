using Transport.Application.Abstractions;
using Transport.Application.Models;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeBackupRepository : IBackupRepository
{
    public int RunCount { get; private set; }
    public List<BackupLog> Logs { get; } = [];
    public BackupJobStatus? JobStatus { get; set; }

    public Task RunBackupAsync(CancellationToken ct = default) { RunCount++; return Task.CompletedTask; }
    public Task<IReadOnlyList<BackupLog>> GetLogsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<BackupLog>>(Logs.ToList());
    public Task<BackupJobStatus?> GetJobStatusAsync(CancellationToken ct = default) => Task.FromResult(JobStatus);
}
