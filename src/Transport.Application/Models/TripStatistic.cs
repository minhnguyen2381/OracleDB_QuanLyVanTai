using Transport.Domain.Enums;

namespace Transport.Application.Models;

// Một dòng của V_TRIP_STATISTICS
public sealed record TripStatistic
{
    public int ScheduleId { get; init; }
    public int RouteId { get; init; }
    public int VehicleId { get; init; }
    public int DriverId { get; init; }
    public required string RouteName { get; init; }
    public required string Origin { get; init; }
    public required string Destination { get; init; }
    public DateTime DepartureTime { get; init; }
    public DateTime ArrivalTime { get; init; }
    public decimal TicketPrice { get; init; }
    public ScheduleStatus Status { get; init; }
    public required string PlateNumber { get; init; }
    public required string VehicleType { get; init; }
    public required string DriverName { get; init; }
    public int SeatCount { get; init; }
    public int SeatsSold { get; init; }
    public int SeatsAvailable { get; init; }
    public decimal OccupancyPct { get; init; }
    public int TicketsCancelled { get; init; }
    public decimal Revenue { get; init; }
}
