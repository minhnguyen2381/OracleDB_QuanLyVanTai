using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Application.Models;
using Transport.Application.Services;
using Transport.Domain.Enums;
using Transport.Presentation.Wpf.Services;

namespace Transport.Presentation.Wpf.ViewModels.Pages;

// Tra cứu vé, thanh toán vé chưa trả, hủy vé (hoàn tiền nếu đã trả), in vé
public sealed partial class TicketsViewModel : PageViewModel
{
    private readonly TicketService _tickets;
    private readonly ITicketPrintService _printer;

    public TicketsViewModel(PageServices services, TicketService tickets, ITicketPrintService printer, TimeProvider time)
        : base(services)
    {
        _tickets = tickets;
        _printer = printer;
        var today = time.GetLocalNow().Date;
        Keyword = string.Empty;
        FromDate = today.AddDays(-7);
        ToDate = today.AddDays(7);
        StatusOptions =
        [
            new FilterOption<TicketStatus?>(null, Loc["Lbl_All"]),
            .. Enum.GetValues<TicketStatus>().Select(s => new FilterOption<TicketStatus?>(s, Loc[$"TicketStatus_{s}"])),
        ];
        SelectedStatus = StatusOptions[0];
    }

    public ObservableCollection<TicketListItem> Tickets { get; } = [];

    public IReadOnlyList<FilterOption<TicketStatus?>> StatusOptions { get; }

    [ObservableProperty]
    public partial string Keyword { get; set; }

    [ObservableProperty]
    public partial DateTime FromDate { get; set; }

    [ObservableProperty]
    public partial DateTime ToDate { get; set; }

    [ObservableProperty]
    public partial FilterOption<TicketStatus?> SelectedStatus { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PayCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelTicketCommand))]
    [NotifyCanExecuteChangedFor(nameof(PrintCommand))]
    public partial TicketListItem? SelectedTicket { get; set; }

    public override Task LoadAsync() => RunBusyAsync(SearchCoreAsync);

    [RelayCommand]
    private Task SearchAsync() => RunBusyAsync(SearchCoreAsync);

    [RelayCommand(CanExecute = nameof(CanPay))]
    private Task PayAsync() => RunBusyAsync(async () =>
    {
        var ticket = SelectedTicket!;
        if (Dialogs.ChoosePaymentMethod(ticket.Price) is not { } method)
        {
            return;
        }

        await _tickets.PayAsync(ticket.TicketId, method);
        Notify.ShowSuccess(Loc.Format("Sell_Paid", ticket.TicketId));
        await SearchCoreAsync();
    });

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private Task CancelTicketAsync() => RunBusyAsync(async () =>
    {
        var ticket = SelectedTicket!;
        var reason = Dialogs.PromptText(Loc.Format("Tickets_CancelTitle", ticket.TicketId), Loc["Tickets_CancelReason"]);
        if (reason is null)
        {
            return;
        }

        await _tickets.CancelAsync(ticket.TicketId, reason);
        Notify.ShowSuccess(ticket.Status == TicketStatus.Paid
            ? Loc.Format("Tickets_CancelledRefund", ticket.Price)
            : Loc["Tickets_Cancelled"]);
        await SearchCoreAsync();
    });

    [RelayCommand(CanExecute = nameof(CanPrint))]
    private Task PrintAsync() => RunBusyAsync(async () =>
    {
        if (await _tickets.GetPrintModelAsync(SelectedTicket!.TicketId) is { } model)
        {
            _printer.Print(model);
        }
    });

    [RelayCommand]
    private Task ExportToExcelAsync() => ExportAsync(Tickets.ToList(),
    [
        new("Tickets_Id", t => t.TicketId),
        new("Tickets_Route", t => t.RouteName),
        new("Tickets_Departure", t => t.DepartureTime, "dd/MM/yyyy HH:mm"),
        new("Tickets_Vehicle", t => t.PlateNumber),
        new("Tickets_Seat", t => t.SeatNo),
        new("Tickets_Passenger", t => t.PassengerName),
        new("Tickets_Phone", t => t.Phone),
        new("Tickets_Price", t => t.Price, "#,##0"),
        new("Tickets_Status", t => Loc[$"TicketStatus_{t.Status}"]),
        new("Tickets_SoldBy", t => t.SoldBy),
    ], "Tickets_ExportTitle");

    private bool CanPay() => SelectedTicket?.Status == TicketStatus.Booked;

    private bool CanCancel() => SelectedTicket is { Status: not TicketStatus.Cancelled };

    private bool CanPrint() => SelectedTicket is { Status: not TicketStatus.Cancelled };

    private async Task SearchCoreAsync()
    {
        var items = await _tickets.SearchAsync(new TicketSearchCriteria
        {
            Keyword = Keyword,
            Status = SelectedStatus.Value,
            From = FromDate.Date,
            To = ToDate.Date.AddDays(1),
        });
        Tickets.Clear();
        foreach (var item in items)
        {
            Tickets.Add(item);
        }
    }
}
