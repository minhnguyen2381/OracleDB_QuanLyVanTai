using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Enums;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeStatisticsQueries : IStatisticsQueries
{
    public List<TripStatistic> Trips { get; } = [];
    public List<DailyRevenue> Daily { get; } = [];
    public TripSearchCriteria? LastCriteria { get; private set; }
    public (DateTime From, DateTime To)? LastDailyRange { get; private set; }

    public Task<IReadOnlyList<TripStatistic>> GetTripsAsync(TripSearchCriteria criteria, CancellationToken ct = default)
    {
        LastCriteria = criteria;
        return Task.FromResult<IReadOnlyList<TripStatistic>>(Trips.ToList());
    }

    public Task<IReadOnlyList<DailyRevenue>> GetDailyRevenueAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        LastDailyRange = (from, to);
        return Task.FromResult<IReadOnlyList<DailyRevenue>>(Daily.ToList());
    }

    // Tạo nhanh một chuyến cho test
    public static TripStatistic Trip(int id, int seatsSold = 0, int seatCount = 40, decimal revenue = 0,
        ScheduleStatus status = ScheduleStatus.Scheduled, decimal price = 280000, DateTime? departure = null,
        int routeId = 1, string routeName = "Hà Nội - Lào Cai") => new()
    {
        ScheduleId = id,
        RouteId = routeId,
        VehicleId = 1,
        DriverId = 1,
        RouteName = routeName,
        Origin = "Hà Nội",
        Destination = "Lào Cai",
        DepartureTime = departure ?? new DateTime(2026, 10, 17, 20, 0, 0),
        ArrivalTime = (departure ?? new DateTime(2026, 10, 17, 20, 0, 0)).AddHours(5.5),
        TicketPrice = price,
        Status = status,
        PlateNumber = "29B-196.88",
        VehicleType = "Giường nằm 40 chỗ",
        DriverName = "Nguyễn Văn An",
        SeatCount = seatCount,
        SeatsSold = seatsSold,
        SeatsAvailable = seatCount - seatsSold,
        OccupancyPct = Math.Round(100m * seatsSold / seatCount, 1),
        Revenue = revenue,
    };
}
