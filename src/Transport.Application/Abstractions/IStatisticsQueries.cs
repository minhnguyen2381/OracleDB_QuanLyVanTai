using Transport.Application.Models;

namespace Transport.Application.Abstractions;

// Đọc view V_TRIP_STATISTICS
public interface IStatisticsQueries
{
    Task<IReadOnlyList<TripStatistic>> GetTripsAsync(TripSearchCriteria criteria, CancellationToken ct = default);
    // Doanh thu theo ngày khởi hành trong [from, to)
    Task<IReadOnlyList<DailyRevenue>> GetDailyRevenueAsync(DateTime from, DateTime to, CancellationToken ct = default);
}
