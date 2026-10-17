namespace Transport.Application.Models;

public sealed record DailyRevenue
{
    public DateTime RevenueDate { get; init; }
    public decimal Revenue { get; init; }
    public int Tickets { get; init; }
}
