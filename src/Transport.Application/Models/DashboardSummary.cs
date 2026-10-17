namespace Transport.Application.Models;

public sealed record DashboardSummary(
    int TripsToday,
    int TicketsSoldToday,
    decimal RevenueToday,
    decimal AverageOccupancyPct,
    IReadOnlyList<DailyRevenue> Last7Days);
