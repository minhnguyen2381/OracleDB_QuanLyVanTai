using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Application.Services;
using Transport.Presentation.Wpf.Services;

namespace Transport.Presentation.Wpf.ViewModels.Pages;

// Bán vé tại quầy: tìm chuyến (kể cả theo khoảng giá) -> chọn ghế -> đặt -> thanh toán -> in vé
public sealed partial class SellTicketViewModel : PageViewModel
{
    private const int SeatsPerRow = 4;

    private readonly IStatisticsQueries _statistics;
    private readonly TicketService _tickets;
    private readonly IPassengerRepository _passengers;
    private readonly ITicketPrintService _printer;

    public SellTicketViewModel(PageServices services, IStatisticsQueries statistics, TicketService tickets,
        IPassengerRepository passengers, ITicketPrintService printer, TimeProvider time)
        : base(services)
    {
        _statistics = statistics;
        _tickets = tickets;
        _passengers = passengers;
        _printer = printer;
        Origin = string.Empty;
        Destination = string.Empty;
        TravelDate = time.GetLocalNow().Date;
        Phone = string.Empty;
        FullName = string.Empty;
        IdCard = string.Empty;
    }

    public ObservableCollection<TripStatistic> Trips { get; } = [];

    public ObservableCollection<SeatRowViewModel> SeatRows { get; } = [];

    [ObservableProperty]
    public partial string Origin { get; set; }

    [ObservableProperty]
    public partial string Destination { get; set; }

    [ObservableProperty]
    public partial DateTime TravelDate { get; set; }

    [ObservableProperty]
    public partial decimal? MinPrice { get; set; }

    [ObservableProperty]
    public partial decimal? MaxPrice { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BookCommand))]
    public partial TripStatistic? SelectedTrip { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BookCommand))]
    public partial SeatViewModel? SelectedSeat { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BookCommand))]
    public partial string Phone { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BookCommand))]
    public partial string FullName { get; set; }

    [ObservableProperty]
    public partial string IdCard { get; set; }

    // "Khách quen ..." / "Khách mới ..." sau khi tra SĐT
    [ObservableProperty]
    public partial string? PassengerHint { get; set; }

    public override Task LoadAsync() => RunBusyAsync(SearchCoreAsync);

    partial void OnSelectedTripChanged(TripStatistic? value) => _ = RunBusyAsync(LoadSeatsAsync);

    [RelayCommand]
    private Task SearchAsync() => RunBusyAsync(SearchCoreAsync);

    [RelayCommand]
    private void SelectSeat(SeatViewModel? seat)
    {
        if (seat is null || seat.IsBooked)
        {
            return;
        }

        if (SelectedSeat is not null)
        {
            SelectedSeat.IsSelected = false;
        }

        seat.IsSelected = true;
        SelectedSeat = seat;
    }

    [RelayCommand]
    private Task LookupPassengerAsync() => RunBusyAsync(async () =>
    {
        if (string.IsNullOrWhiteSpace(Phone))
        {
            return;
        }

        var passenger = await _passengers.FindByPhoneAsync(Phone.Trim());
        if (passenger is null)
        {
            PassengerHint = Loc["Sell_NewPassenger"];
            return;
        }

        FullName = passenger.FullName;
        IdCard = passenger.IdCard ?? string.Empty;
        PassengerHint = Loc.Format("Sell_FoundPassenger", passenger.FullName);
    });

    [RelayCommand(CanExecute = nameof(CanBook))]
    private Task BookAsync()
    {
        var trip = SelectedTrip!;
        var seat = SelectedSeat!;
        // Số ghế được truyền vào bản dịch lỗi "Ghế {0} đã có người đặt."
        return RunBusyAsync(async () =>
        {
            var ticketId = await _tickets.BookAsync(new BookingRequest(trip.ScheduleId, FullName, Phone, IdCard, seat.Number));
            Notify.ShowSuccess(Loc.Format("Sell_Booked", ticketId, seat.Number));

            if (Dialogs.ChoosePaymentMethod(trip.TicketPrice) is { } method)
            {
                await _tickets.PayAsync(ticketId, method);
                Notify.ShowSuccess(Loc.Format("Sell_Paid", ticketId));
            }

            if (Dialogs.Confirm(Loc["Sell_ConfirmPrint"]) && await _tickets.GetPrintModelAsync(ticketId) is { } ticket)
            {
                _printer.Print(ticket);
            }

            Phone = string.Empty;
            FullName = string.Empty;
            IdCard = string.Empty;
            PassengerHint = null;
            await SearchCoreAsync();
        }, seat.Number);
    }

    private bool CanBook() =>
        SelectedTrip is not null && SelectedSeat is not null
        && !string.IsNullOrWhiteSpace(Phone) && !string.IsNullOrWhiteSpace(FullName);

    // Tải lại danh sách chuyến; giữ chuyến đang chọn và nạp lại sơ đồ ghế của nó
    private async Task SearchCoreAsync()
    {
        var selectedId = SelectedTrip?.ScheduleId;
        SelectedTrip = null;
        var trips = await _statistics.GetTripsAsync(new TripSearchCriteria
        {
            From = TravelDate.Date,
            To = TravelDate.Date.AddDays(1),
            Origin = Origin,
            Destination = Destination,
            MinPrice = MinPrice,
            MaxPrice = MaxPrice,
            OnlyOpenForSale = true,
        });
        Trips.Clear();
        foreach (var trip in trips)
        {
            Trips.Add(trip);
        }

        SelectedTrip = Trips.FirstOrDefault(t => t.ScheduleId == selectedId);
    }

    private async Task LoadSeatsAsync()
    {
        SelectedSeat = null;
        SeatRows.Clear();
        if (SelectedTrip is not { } trip)
        {
            return;
        }

        var booked = await _tickets.GetBookedSeatsAsync(trip.ScheduleId);
        var seats = Enumerable.Range(1, trip.SeatCount).Select(n => new SeatViewModel(n, booked.Contains(n)));
        foreach (var row in seats.Chunk(SeatsPerRow))
        {
            SeatRows.Add(new SeatRowViewModel([.. row.Take(2)], [.. row.Skip(2)]));
        }
    }
}
