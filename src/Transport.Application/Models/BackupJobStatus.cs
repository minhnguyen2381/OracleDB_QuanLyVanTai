namespace Transport.Application.Models;

// Trạng thái JOB_NIGHTLY_BACKUP (view V_BACKUP_JOB_STATUS)
public sealed record BackupJobStatus
{
    public required string JobName { get; init; }
    // 'TRUE' / 'FALSE' như Oracle trả về
    public required string Enabled { get; init; }
    public string? State { get; init; }
    public DateTime? LastStartDate { get; init; }
    public DateTime? NextRunDate { get; init; }
    public int? FailureCount { get; init; }

    public bool IsEnabled => Enabled == "TRUE";
}
