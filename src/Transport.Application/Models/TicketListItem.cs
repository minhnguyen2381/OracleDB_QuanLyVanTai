using Transport.Domain.Enums;

namespace Transport.Application.Models;

public sealed record TicketListItem
{
    public long TicketId { get; init; }
    public int ScheduleId { get; init; }
    public required string RouteName { get; init; }
    public DateTime DepartureTime { get; init; }
    public required string PlateNumber { get; init; }
    public int SeatNo { get; init; }
    public required string PassengerName { get; init; }
    public required string Phone { get; init; }
    public decimal Price { get; init; }
    public TicketStatus Status { get; init; }
    public DateTime BookedAt { get; init; }
    public required string SoldBy { get; init; }
    public string? CancelReason { get; init; }
}
