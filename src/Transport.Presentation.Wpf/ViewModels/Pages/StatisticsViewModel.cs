using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Enums;

namespace Transport.Presentation.Wpf.ViewModels.Pages;

// Thống kê chuyến xe từ view V_TRIP_STATISTICS
public sealed partial class StatisticsViewModel : PageViewModel
{
    private readonly IStatisticsQueries _statistics;
    private readonly IRouteRepository _routes;

    public StatisticsViewModel(PageServices services, IStatisticsQueries statistics, IRouteRepository routes, TimeProvider time)
        : base(services)
    {
        _statistics = statistics;
        _routes = routes;
        var today = time.GetLocalNow().Date;
        FromDate = new DateTime(today.Year, today.Month, 1);
        ToDate = today;
        RouteOptions = [new FilterOption<int?>(null, Loc["Stats_AllRoutes"])];
        SelectedRoute = RouteOptions[0];
        RevenueByRoute = [];
    }

    public ObservableCollection<TripStatistic> Trips { get; } = [];

    [ObservableProperty]
    public partial DateTime FromDate { get; set; }

    [ObservableProperty]
    public partial DateTime ToDate { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<FilterOption<int?>> RouteOptions { get; set; }

    [ObservableProperty]
    public partial FilterOption<int?> SelectedRoute { get; set; }

    [ObservableProperty]
    public partial int TotalTrips { get; set; }

    [ObservableProperty]
    public partial int TotalTicketsSold { get; set; }

    [ObservableProperty]
    public partial decimal TotalRevenue { get; set; }

    [ObservableProperty]
    public partial decimal AverageOccupancy { get; set; }

    [ObservableProperty]
    public partial int TotalCancelled { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<ChartBar> RevenueByRoute { get; set; }

    public override Task LoadAsync() => RunBusyAsync(async () =>
    {
        var routes = await _routes.GetAllAsync();
        RouteOptions =
        [
            new FilterOption<int?>(null, Loc["Stats_AllRoutes"]),
            .. routes.Select(r => new FilterOption<int?>(r.Id, r.Name)),
        ];
        SelectedRoute = RouteOptions[0];
        await SearchCoreAsync();
    });

    [RelayCommand]
    private Task SearchAsync() => RunBusyAsync(SearchCoreAsync);

    [RelayCommand]
    private Task ExportToExcelAsync() => ExportAsync(Trips.ToList(),
    [
        new("Schedules_Departure", t => t.DepartureTime, "dd/MM/yyyy HH:mm"),
        new("Schedules_Route", t => t.RouteName),
        new("Schedules_Vehicle", t => t.PlateNumber),
        new("Schedules_Driver", t => t.DriverName),
        new("Schedules_SeatCount", t => t.SeatCount),
        new("Schedules_Sold", t => t.SeatsSold),
        new("Stats_Occupancy", t => t.OccupancyPct),
        new("Stats_Cancelled", t => t.TicketsCancelled),
        new("Stats_Revenue", t => t.Revenue, "#,##0"),
    ], "Stats_ExportTitle");

    private async Task SearchCoreAsync()
    {
        var trips = await _statistics.GetTripsAsync(new TripSearchCriteria
        {
            From = FromDate.Date,
            To = ToDate.Date.AddDays(1),
            RouteId = SelectedRoute.Value,
        });
        Trips.Clear();
        foreach (var trip in trips)
        {
            Trips.Add(trip);
        }

        // Chuyến đã hủy không tính vào số chuyến / vé bán / tỷ lệ lấp đầy
        var active = trips.Where(t => t.Status != ScheduleStatus.Cancelled).ToList();
        TotalTrips = active.Count;
        TotalTicketsSold = active.Sum(t => t.SeatsSold);
        TotalRevenue = trips.Sum(t => t.Revenue);
        AverageOccupancy = active.Count == 0 ? 0 : Math.Round(active.Average(t => t.OccupancyPct), 1);
        TotalCancelled = trips.Sum(t => t.TicketsCancelled);
        RevenueByRoute = ChartBar.Build(trips
            .GroupBy(t => t.RouteName)
            .Select(g => (g.Key, g.Sum(t => t.Revenue)))
            .OrderByDescending(p => p.Item2));
    }
}
