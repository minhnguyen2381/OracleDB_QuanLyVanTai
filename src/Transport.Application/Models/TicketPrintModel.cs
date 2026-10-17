using Transport.Domain.Enums;

namespace Transport.Application.Models;

// Dữ liệu in lên vé
public sealed record TicketPrintModel
{
    public long TicketId { get; init; }
    public required string RouteName { get; init; }
    public required string Origin { get; init; }
    public required string Destination { get; init; }
    public DateTime DepartureTime { get; init; }
    public DateTime ArrivalTime { get; init; }
    public required string PlateNumber { get; init; }
    public required string VehicleType { get; init; }
    public int SeatNo { get; init; }
    public required string PassengerName { get; init; }
    public required string Phone { get; init; }
    public decimal Price { get; init; }
    public TicketStatus Status { get; init; }
    public required string SoldBy { get; init; }
    public DateTime BookedAt { get; init; }
}
