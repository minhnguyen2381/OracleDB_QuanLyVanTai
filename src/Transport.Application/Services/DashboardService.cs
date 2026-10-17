using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Enums;

namespace Transport.Application.Services;

// Số liệu màn Tổng quan: hôm nay + doanh thu 7 ngày gần nhất
public sealed class DashboardService(IStatisticsQueries statistics, TimeProvider time)
{
    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken ct = default)
    {
        var today = time.GetLocalNow().Date;
        var trips = await statistics.GetTripsAsync(new TripSearchCriteria { From = today, To = today.AddDays(1) }, ct);
        var active = trips.Where(t => t.Status != ScheduleStatus.Cancelled).ToList();

        var daily = await statistics.GetDailyRevenueAsync(today.AddDays(-6), today.AddDays(1), ct);
        var last7Days = Enumerable.Range(0, 7)
            .Select(i => today.AddDays(i - 6))
            .Select(day => daily.FirstOrDefault(d => d.RevenueDate.Date == day) ?? new DailyRevenue { RevenueDate = day })
            .ToList();

        return new DashboardSummary(
            active.Count,
            active.Sum(t => t.SeatsSold),
            active.Sum(t => t.Revenue),
            active.Count == 0 ? 0 : Math.Round(active.Average(t => t.OccupancyPct), 1),
            last7Days);
    }
}
