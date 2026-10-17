namespace Transport.Domain.Entities;

// Hành khách
public sealed record Passenger
{
    public int Id { get; init; }
    public required string FullName { get; init; }
    public required string Phone { get; init; }
    public string? IdCard { get; init; }
    public string? Email { get; init; }
}
