using System.ComponentModel.DataAnnotations;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Application.Services;
using Transport.Domain.Entities;
using Transport.Domain.Enums;

namespace Transport.Presentation.Wpf.ViewModels.Pages;

// Lịch chạy: danh sách đọc từ V_TRIP_STATISTICS (seller xem được), điều hành thêm/sửa/hủy/đổi trạng thái
public sealed partial class SchedulesViewModel : CrudPageViewModel<TripStatistic>
{
    private const string TimePattern = @"^([01]\d|2[0-3]):[0-5]\d$";

    private readonly IStatisticsQueries _statistics;
    private readonly ScheduleService _schedules;
    private readonly IRouteRepository _routes;
    private readonly IVehicleRepository _vehicles;
    private readonly IDriverRepository _drivers;
    private readonly DateTime _today;
    private IReadOnlyList<Driver> _allDrivers = [];
    private int _editingId;

    public SchedulesViewModel(PageServices services, IStatisticsQueries statistics, ScheduleService schedules,
        IRouteRepository routes, IVehicleRepository vehicles, IDriverRepository drivers, TimeProvider time)
        : base(services)
    {
        _statistics = statistics;
        _schedules = schedules;
        _routes = routes;
        _vehicles = vehicles;
        _drivers = drivers;
        _today = time.GetLocalNow().Date;
        FromDate = _today;
        ToDate = _today.AddDays(7);
        Routes = [];
        Vehicles = [];
        AvailableDrivers = [];
        DepartureDate = _today.AddDays(1);
        DepartureTimeText = "07:00";
        ArrivalDate = _today.AddDays(1);
        ArrivalTimeText = "12:00";
    }

    [ObservableProperty]
    public partial DateTime FromDate { get; set; }

    [ObservableProperty]
    public partial DateTime ToDate { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<Route> Routes { get; set; }

    // Chỉ xe đang hoạt động
    [ObservableProperty]
    public partial IReadOnlyList<Vehicle> Vehicles { get; set; }

    // Tài xế đang làm việc và còn hạn bằng tại ngày khởi hành
    [ObservableProperty]
    public partial IReadOnlyList<Driver> AvailableDrivers { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Val_Required")]
    public partial Route? SelectedRoute { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Val_Required")]
    public partial Vehicle? SelectedVehicle { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Val_Required")]
    public partial Driver? SelectedDriver { get; set; }

    [ObservableProperty]
    public partial DateTime DepartureDate { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Val_Required")]
    [RegularExpression(TimePattern, ErrorMessage = "Val_Time")]
    public partial string DepartureTimeText { get; set; }

    [ObservableProperty]
    public partial DateTime ArrivalDate { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Val_Required")]
    [RegularExpression(TimePattern, ErrorMessage = "Val_Time")]
    public partial string ArrivalTimeText { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(typeof(decimal), "1", "100000000", ErrorMessage = "Val_Positive")]
    public partial decimal TicketPrice { get; set; }

    protected override string ExportTitleKey => "Schedules_ExportTitle";

    protected override IReadOnlyList<ExportColumn<TripStatistic>> ExportColumns =>
    [
        new("Schedules_Departure", t => t.DepartureTime, "dd/MM/yyyy HH:mm"),
        new("Schedules_Arrival", t => t.ArrivalTime, "dd/MM/yyyy HH:mm"),
        new("Schedules_Route", t => t.RouteName),
        new("Schedules_Vehicle", t => t.PlateNumber),
        new("Schedules_Driver", t => t.DriverName),
        new("Schedules_Price", t => t.TicketPrice, "#,##0"),
        new("Schedules_Sold", t => t.SeatsSold),
        new("Schedules_SeatCount", t => t.SeatCount),
        new("Schedules_Status", t => Loc[$"ScheduleStatus_{t.Status}"]),
    ];

    public override async Task LoadAsync()
    {
        // Seller không có quyền đọc bảng DRIVERS nên chỉ điều hành mới tải danh mục cho form
        if (IsOperator)
        {
            await RunBusyAsync(LoadLookupsAsync);
        }

        await base.LoadAsync();
    }

    protected override Task<IReadOnlyList<TripStatistic>> LoadItemsAsync() =>
        _statistics.GetTripsAsync(new TripSearchCriteria { From = FromDate.Date, To = ToDate.Date.AddDays(1) });

    protected override bool Matches(TripStatistic item, string text) =>
        Contains(item.RouteName, text) || Contains(item.PlateNumber, text) || Contains(item.DriverName, text);

    // Chỉ sửa/xóa chuyến chưa chạy; xóa thêm điều kiện chưa có vé nào
    protected override bool CanEditItem(TripStatistic item) => item.Status == ScheduleStatus.Scheduled;

    protected override bool CanDeleteItem(TripStatistic item) =>
        item.Status == ScheduleStatus.Scheduled && item.SeatsSold == 0 && item.TicketsCancelled == 0;

    protected override void OnSelectionChanged(TripStatistic? item)
    {
        CancelTripCommand.NotifyCanExecuteChanged();
        MarkRunningCommand.NotifyCanExecuteChanged();
        MarkCompletedCommand.NotifyCanExecuteChanged();
    }

    protected override void FillForm(TripStatistic? item)
    {
        _editingId = item?.ScheduleId ?? 0;
        if (item is null)
        {
            SelectedRoute = null;
            SelectedVehicle = null;
            SelectedDriver = null;
            DepartureDate = _today.AddDays(1);
            DepartureTimeText = "07:00";
            ArrivalDate = _today.AddDays(1);
            ArrivalTimeText = "12:00";
            TicketPrice = 0;
            return;
        }

        DepartureDate = item.DepartureTime.Date;
        DepartureTimeText = item.DepartureTime.ToString("HH:mm", CultureInfo.InvariantCulture);
        ArrivalDate = item.ArrivalTime.Date;
        ArrivalTimeText = item.ArrivalTime.ToString("HH:mm", CultureInfo.InvariantCulture);
        TicketPrice = item.TicketPrice;
        SelectedRoute = Routes.FirstOrDefault(r => r.Id == item.RouteId);
        SelectedVehicle = Vehicles.FirstOrDefault(v => v.Id == item.VehicleId);
        SelectedDriver = AvailableDrivers.FirstOrDefault(d => d.Id == item.DriverId);
    }

    protected override Task SaveFormAsync(bool isNew)
    {
        // Giờ đã được kiểm tra bằng TimePattern nên ghép luôn được
        TryCombine(DepartureDate, DepartureTimeText, out var departure);
        TryCombine(ArrivalDate, ArrivalTimeText, out var arrival);
        var schedule = new Schedule
        {
            Id = _editingId,
            RouteId = SelectedRoute!.Id,
            VehicleId = SelectedVehicle!.Id,
            DriverId = SelectedDriver!.Id,
            DepartureTime = departure,
            ArrivalTime = arrival,
            TicketPrice = TicketPrice,
        };
        return isNew ? _schedules.AddAsync(schedule) : _schedules.UpdateAsync(schedule);
    }

    protected override Task DeleteItemAsync(TripStatistic item) => _schedules.DeleteAsync(item.ScheduleId);

    protected override string Describe(TripStatistic item) =>
        $"{item.RouteName} {item.DepartureTime.ToString("dd/MM HH:mm", CultureInfo.InvariantCulture)}";

    // Chọn tuyến khi thêm mới: lấy giá cơ bản và tự tính giờ đến
    partial void OnSelectedRouteChanged(Route? value)
    {
        if (IsNewItem && value is not null)
        {
            TicketPrice = value.BasePrice;
            UpdateArrival();
        }
    }

    partial void OnDepartureDateChanged(DateTime value)
    {
        RefreshAvailableDrivers();
        if (IsNewItem)
        {
            UpdateArrival();
        }
    }

    partial void OnDepartureTimeTextChanged(string value)
    {
        if (IsNewItem)
        {
            UpdateArrival();
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancelTrip))]
    private Task CancelTripAsync() => RunBusyAsync(async () =>
    {
        var trip = SelectedItem!;
        var reason = Dialogs.PromptText(Loc["Schedules_CancelTitle"], Loc["Schedules_CancelReason"]);
        if (reason is null)
        {
            return;
        }

        await _schedules.CancelAsync(trip.ScheduleId, reason);
        Notify.ShowSuccess(Loc["Schedules_Cancelled"]);
        await ReloadAsync();
    });

    [RelayCommand(CanExecute = nameof(CanMarkRunning))]
    private Task MarkRunningAsync() => ChangeStatusAsync(id => _schedules.MarkRunningAsync(id));

    [RelayCommand(CanExecute = nameof(CanMarkCompleted))]
    private Task MarkCompletedAsync() => ChangeStatusAsync(id => _schedules.MarkCompletedAsync(id));

    private Task ChangeStatusAsync(Func<int, Task> change) => RunBusyAsync(async () =>
    {
        await change(SelectedItem!.ScheduleId);
        Notify.ShowSuccess(Loc["Schedules_StatusUpdated"]);
        await ReloadAsync();
    });

    private bool CanCancelTrip() => IsOperator && SelectedItem?.Status == ScheduleStatus.Scheduled;

    private bool CanMarkRunning() => IsOperator && SelectedItem?.Status == ScheduleStatus.Scheduled;

    private bool CanMarkCompleted() => IsOperator && SelectedItem?.Status == ScheduleStatus.Running;

    private async Task LoadLookupsAsync()
    {
        Routes = await _routes.GetAllAsync();
        Vehicles = [.. (await _vehicles.GetAllAsync()).Where(v => v.Status == VehicleStatus.Active)];
        _allDrivers = await _drivers.GetAllAsync();
        RefreshAvailableDrivers();
    }

    private void RefreshAvailableDrivers()
    {
        var current = SelectedDriver;
        AvailableDrivers = [.. _allDrivers.Where(d => d.CanDrive(DepartureDate))];
        SelectedDriver = current is null ? null : AvailableDrivers.FirstOrDefault(d => d.Id == current.Id);
    }

    private void UpdateArrival()
    {
        if (SelectedRoute?.DurationMin is not { } minutes || !TryCombine(DepartureDate, DepartureTimeText, out var departure))
        {
            return;
        }

        var arrival = departure.AddMinutes(minutes);
        ArrivalDate = arrival.Date;
        ArrivalTimeText = arrival.ToString("HH:mm", CultureInfo.InvariantCulture);
    }

    private static bool TryCombine(DateTime date, string time, out DateTime result)
    {
        result = date.Date;
        if (!TimeSpan.TryParseExact(time, @"hh\:mm", CultureInfo.InvariantCulture, out var span))
        {
            return false;
        }

        result = date.Date + span;
        return true;
    }
}
