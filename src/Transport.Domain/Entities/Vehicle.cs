using Transport.Domain.Enums;

namespace Transport.Domain.Entities;

// Phương tiện
public sealed record Vehicle
{
    public int Id { get; init; }
    public required string PlateNumber { get; init; }
    public required string VehicleType { get; init; }
    public int SeatCount { get; init; }
    public VehicleStatus Status { get; init; }
}
