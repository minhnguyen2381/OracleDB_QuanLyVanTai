namespace Transport.Application.Models;

// Một dòng V_LOGIN_AUDIT
public sealed record LoginAuditEntry
{
    public DateTime EventTime { get; init; }
    public required string Username { get; init; }
    public required string ActionName { get; init; }
    // 'SUCCESS' hoặc 'FAILED'
    public required string Result { get; init; }
    public int ReturnCode { get; init; }
    public string? UserHost { get; init; }
    public string? ObjectName { get; init; }
}
