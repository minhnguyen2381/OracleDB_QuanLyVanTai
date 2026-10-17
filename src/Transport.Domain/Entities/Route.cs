namespace Transport.Domain.Entities;

// Tuyến xe
public sealed record Route
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Origin { get; init; }
    public required string Destination { get; init; }
    public int? DistanceKm { get; init; }
    public int? DurationMin { get; init; }
    public decimal BasePrice { get; init; }
}
