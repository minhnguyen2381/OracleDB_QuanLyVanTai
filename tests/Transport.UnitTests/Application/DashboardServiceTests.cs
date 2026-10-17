using Transport.Application.Models;
using Transport.Application.Services;
using Transport.Domain.Enums;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Application;

public class DashboardServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 17, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetSummaryAsync_AggregatesTodayAndFillsSevenDays()
    {
        var queries = new FakeStatisticsQueries();
        queries.Trips.Add(FakeStatisticsQueries.Trip(1, seatsSold: 20, seatCount: 40, revenue: 5_600_000));
        queries.Trips.Add(FakeStatisticsQueries.Trip(2, seatsSold: 4, seatCount: 16, revenue: 400_000));
        queries.Trips.Add(FakeStatisticsQueries.Trip(3, seatsSold: 0, seatCount: 45, status: ScheduleStatus.Cancelled));
        queries.Daily.Add(new DailyRevenue { RevenueDate = new DateTime(2026, 10, 15), Revenue = 1_000_000, Tickets = 5 });

        var summary = await new DashboardService(queries, new FixedTimeProvider(Now)).GetSummaryAsync();

        Assert.Equal(new DateTime(2026, 10, 17), queries.LastCriteria!.From);
        Assert.Equal(new DateTime(2026, 10, 18), queries.LastCriteria.To);
        Assert.Equal(2, summary.TripsToday);
        Assert.Equal(24, summary.TicketsSoldToday);
        Assert.Equal(6_000_000, summary.RevenueToday);
        Assert.Equal(37.5m, summary.AverageOccupancyPct);
        Assert.Equal(7, summary.Last7Days.Count);
        Assert.Equal(new DateTime(2026, 10, 11), summary.Last7Days[0].RevenueDate);
        Assert.Equal(1_000_000, summary.Last7Days[4].Revenue);
        Assert.Equal(0, summary.Last7Days[6].Revenue);
    }
}
