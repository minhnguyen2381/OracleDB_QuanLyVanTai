using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Application.Services;

namespace Transport.Presentation.Wpf.ViewModels.Pages;

// Tổng quan hôm nay + doanh thu 7 ngày gần nhất
public sealed partial class DashboardViewModel : PageViewModel
{
    private readonly DashboardService _dashboard;

    public DashboardViewModel(PageServices services, DashboardService dashboard)
        : base(services)
    {
        _dashboard = dashboard;
        Last7Days = [];
    }

    [ObservableProperty]
    public partial int TripsToday { get; set; }

    [ObservableProperty]
    public partial int TicketsSoldToday { get; set; }

    [ObservableProperty]
    public partial decimal RevenueToday { get; set; }

    [ObservableProperty]
    public partial decimal AverageOccupancy { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<ChartBar> Last7Days { get; set; }

    public override Task LoadAsync() => RunBusyAsync(LoadCoreAsync);

    [RelayCommand]
    private Task RefreshAsync() => RunBusyAsync(LoadCoreAsync);

    private async Task LoadCoreAsync()
    {
        var summary = await _dashboard.GetSummaryAsync();
        TripsToday = summary.TripsToday;
        TicketsSoldToday = summary.TicketsSoldToday;
        RevenueToday = summary.RevenueToday;
        AverageOccupancy = summary.AverageOccupancyPct;
        Last7Days = ChartBar.Build(summary.Last7Days.Select(d =>
            (d.RevenueDate.ToString("dd/MM", CultureInfo.InvariantCulture), d.Revenue)));
    }
}
