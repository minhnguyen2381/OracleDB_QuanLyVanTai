using Transport.Domain.Enums;

namespace Transport.Application.Models;

public sealed record TicketSearchCriteria
{
    // Mã vé, SĐT hoặc một phần tên khách
    public string? Keyword { get; init; }
    public TicketStatus? Status { get; init; }
    public int? ScheduleId { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
}
