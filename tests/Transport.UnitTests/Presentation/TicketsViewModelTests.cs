using Transport.Application.Services;
using Transport.Domain.Enums;
using Transport.Presentation.Wpf.ViewModels.Pages;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class TicketsViewModelTests
{
    private static readonly FixedTimeProvider Time = new(new DateTimeOffset(2026, 10, 17, 8, 0, 0, TimeSpan.Zero));
    private readonly PageTestContext _context = new();
    private readonly FakeTicketRepository _tickets = new();
    private readonly FakeTicketPrintService _printer = new();

    public TicketsViewModelTests()
    {
        _tickets.SearchResult.Add(TestData.Ticket(1, TicketStatus.Booked));
        _tickets.SearchResult.Add(TestData.Ticket(2, TicketStatus.Paid, 280000));
        _tickets.SearchResult.Add(TestData.Ticket(3, TicketStatus.Cancelled));
    }

    private TicketsViewModel Create() => new(_context.Services, new TicketService(_tickets), _printer, Time);

    [Fact]
    public async Task Load_SearchesLastAndNextSevenDays()
    {
        var vm = Create();

        await vm.LoadAsync();

        var criteria = _tickets.LastSearch!;
        Assert.Equal(new DateTime(2026, 10, 10), criteria.From);
        Assert.Equal(new DateTime(2026, 10, 25), criteria.To);
        Assert.Null(criteria.Status);
        Assert.Equal(3, vm.Tickets.Count);
    }

    [Fact]
    public async Task Search_PassesKeywordAndStatus()
    {
        var vm = Create();
        vm.Keyword = " 0987 ";
        vm.SelectedStatus = vm.StatusOptions[2];

        await vm.SearchCommand.ExecuteAsync(null);

        Assert.Equal("0987", _tickets.LastSearch!.Keyword);
        Assert.Equal(TicketStatus.Paid, _tickets.LastSearch.Status);
        Assert.Equal("Lbl_All", vm.StatusOptions[0].Label);
    }

    [Fact]
    public async Task Pay_OnlyForUnpaidTickets_UsingChosenMethod()
    {
        var vm = Create();
        await vm.LoadAsync();

        vm.SelectedTicket = vm.Tickets[1];
        Assert.False(vm.PayCommand.CanExecute(null));
        vm.SelectedTicket = vm.Tickets[0];
        Assert.True(vm.PayCommand.CanExecute(null));

        _context.Dialogs.PaymentResult = PaymentMethod.Card;
        await vm.PayCommand.ExecuteAsync(null);

        Assert.Equal((1L, PaymentMethod.Card), Assert.Single(_tickets.Payments));
    }

    [Fact]
    public async Task CancelPaidTicket_AsksReasonAndReportsRefund()
    {
        var vm = Create();
        await vm.LoadAsync();
        vm.SelectedTicket = vm.Tickets[1];
        _context.Dialogs.PromptTextResult = "Khách đổi ý";

        await vm.CancelTicketCommand.ExecuteAsync(null);

        Assert.Equal((2L, "Khách đổi ý"), Assert.Single(_tickets.Cancellations));
        Assert.Equal("Tickets_CancelTitle|2", _context.Dialogs.Prompts[0]);
        Assert.Equal(new[] { "Tickets_CancelledRefund|280000" }, _context.Notifications.Successes);
    }

    [Fact]
    public async Task CancelledTicket_CannotBeCancelledOrPrinted()
    {
        var vm = Create();
        await vm.LoadAsync();

        vm.SelectedTicket = vm.Tickets[2];

        Assert.False(vm.CancelTicketCommand.CanExecute(null));
        Assert.False(vm.PrintCommand.CanExecute(null));
    }

    [Fact]
    public async Task Print_SendsTicketToPrinter()
    {
        _tickets.PrintModel = TestData.PrintModel(1);
        var vm = Create();
        await vm.LoadAsync();
        vm.SelectedTicket = vm.Tickets[0];

        await vm.PrintCommand.ExecuteAsync(null);

        Assert.Equal(1, Assert.Single(_printer.Printed).TicketId);
    }

    [Fact]
    public async Task Export_WritesAllTicketColumns()
    {
        var vm = Create();
        await vm.LoadAsync();

        await vm.ExportToExcelCommand.ExecuteAsync(null);

        Assert.Equal("Tickets_ExportTitle", _context.Exporter.Title);
        Assert.Equal(10, _context.Exporter.Headers.Count);
        Assert.Equal(3, _context.Exporter.RowCount);
    }
}
