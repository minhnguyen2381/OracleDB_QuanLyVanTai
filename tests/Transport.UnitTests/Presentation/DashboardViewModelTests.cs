using Transport.Application.Models;
using Transport.Application.Services;
using Transport.Presentation.Wpf.ViewModels.Pages;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class DashboardViewModelTests
{
    [Fact]
    public async Task Load_ShowsTodayFiguresAndSevenDailyBars()
    {
        var statistics = new FakeStatisticsQueries();
        statistics.Trips.Add(FakeStatisticsQueries.Trip(1, seatsSold: 20, seatCount: 40, revenue: 5_600_000));
        statistics.Daily.Add(new DailyRevenue { RevenueDate = new DateTime(2026, 10, 16), Revenue = 2_000_000, Tickets = 8 });
        var time = new FixedTimeProvider(new DateTimeOffset(2026, 10, 17, 8, 0, 0, TimeSpan.Zero));
        var vm = new DashboardViewModel(new PageTestContext().Services, new DashboardService(statistics, time));

        await vm.LoadAsync();

        Assert.Equal(1, vm.TripsToday);
        Assert.Equal(20, vm.TicketsSoldToday);
        Assert.Equal(5_600_000, vm.RevenueToday);
        Assert.Equal(50m, vm.AverageOccupancy);
        Assert.Equal(7, vm.Last7Days.Count);
        Assert.Equal("11/10", vm.Last7Days[0].Label);
        Assert.Equal("17/10", vm.Last7Days[6].Label);
        Assert.Equal(150d, vm.Last7Days[5].Height);
    }
}
