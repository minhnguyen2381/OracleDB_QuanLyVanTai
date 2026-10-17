using Transport.Domain.Enums;
using Transport.Presentation.Wpf.ViewModels.Pages;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class StatisticsViewModelTests
{
    private static readonly FixedTimeProvider Time = new(new DateTimeOffset(2026, 10, 17, 8, 0, 0, TimeSpan.Zero));
    private readonly PageTestContext _context = new();
    private readonly FakeStatisticsQueries _statistics = new();
    private readonly FakeRouteRepository _routes = new();

    public StatisticsViewModelTests()
    {
        _routes.Items.Add(TestData.Route(1, "Hà Nội - Lào Cai"));
        _routes.Items.Add(TestData.Route(2, "Hà Nội - Hải Phòng"));
        _statistics.Trips.Add(FakeStatisticsQueries.Trip(1, seatsSold: 20, seatCount: 40, revenue: 5_600_000, routeId: 1, routeName: "Hà Nội - Lào Cai"));
        _statistics.Trips.Add(FakeStatisticsQueries.Trip(2, seatsSold: 10, seatCount: 40, revenue: 1_200_000, routeId: 2, routeName: "Hà Nội - Hải Phòng"));
        _statistics.Trips.Add(FakeStatisticsQueries.Trip(3, status: ScheduleStatus.Cancelled, routeId: 2, routeName: "Hà Nội - Hải Phòng")
            with { TicketsCancelled = 4 });
    }

    private StatisticsViewModel Create() => new(_context.Services, _statistics, _routes, Time);

    [Fact]
    public async Task Load_DefaultsToCurrentMonth_AndComputesTotals()
    {
        var vm = Create();

        await vm.LoadAsync();

        Assert.Equal(new DateTime(2026, 10, 1), _statistics.LastCriteria!.From);
        Assert.Equal(new DateTime(2026, 10, 18), _statistics.LastCriteria.To);
        Assert.Null(_statistics.LastCriteria.RouteId);
        Assert.Equal(2, vm.TotalTrips);
        Assert.Equal(30, vm.TotalTicketsSold);
        Assert.Equal(6_800_000, vm.TotalRevenue);
        Assert.Equal(37.5m, vm.AverageOccupancy);
        Assert.Equal(4, vm.TotalCancelled);
        Assert.Equal("Hà Nội - Lào Cai", vm.RevenueByRoute[0].Label);
        Assert.Equal(150d, vm.RevenueByRoute[0].Height);
        Assert.Equal(3, vm.RouteOptions.Count);
        Assert.Equal("Stats_AllRoutes", vm.RouteOptions[0].Label);
    }

    [Fact]
    public async Task SelectingRoute_FiltersQuery()
    {
        var vm = Create();
        await vm.LoadAsync();

        vm.SelectedRoute = vm.RouteOptions[1];
        await vm.SearchCommand.ExecuteAsync(null);

        Assert.Equal(1, _statistics.LastCriteria!.RouteId);
    }

    [Fact]
    public async Task Export_WritesTripStatistics()
    {
        var vm = Create();
        await vm.LoadAsync();

        await vm.ExportToExcelCommand.ExecuteAsync(null);

        Assert.Equal("Stats_ExportTitle", _context.Exporter.Title);
        Assert.Equal(9, _context.Exporter.Headers.Count);
        Assert.Equal(3, _context.Exporter.RowCount);
    }
}
