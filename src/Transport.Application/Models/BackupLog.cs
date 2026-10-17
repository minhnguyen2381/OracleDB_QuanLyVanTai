namespace Transport.Application.Models;

public sealed record BackupLog
{
    public int LogId { get; init; }
    public DateTime BackupAt { get; init; }
    public required string PerformedBy { get; init; }
    public int? ScheduleRows { get; init; }
    public int? TicketRows { get; init; }
    public int? PaymentRows { get; init; }
}
