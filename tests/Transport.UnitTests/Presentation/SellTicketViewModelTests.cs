using Transport.Application.Models;
using Transport.Application.Services;
using Transport.Domain.Enums;
using Transport.Domain.Errors;
using Transport.Presentation.Wpf.ViewModels.Pages;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class SellTicketViewModelTests
{
    private static readonly FixedTimeProvider Time = new(new DateTimeOffset(2026, 10, 17, 8, 0, 0, TimeSpan.Zero));
    private readonly PageTestContext _context = new();
    private readonly FakeStatisticsQueries _statistics = new();
    private readonly FakeTicketRepository _tickets = new() { NextTicketId = 100 };
    private readonly FakePassengerRepository _passengers = new();
    private readonly FakeTicketPrintService _printer = new();

    public SellTicketViewModelTests()
    {
        _statistics.Trips.Add(FakeStatisticsQueries.Trip(7, seatsSold: 2, seatCount: 16, price: 280000,
            departure: new DateTime(2026, 10, 17, 20, 0, 0)));
        _tickets.BookedSeats.UnionWith([1, 2]);
    }

    private SellTicketViewModel Create() =>
        new(_context.Services, _statistics, new TicketService(_tickets), _passengers, _printer, Time);

    private async Task<SellTicketViewModel> CreateWithTripSelectedAsync()
    {
        var vm = Create();
        await vm.LoadAsync();
        vm.SelectedTrip = vm.Trips[0];
        return vm;
    }

    [Fact]
    public async Task Search_UsesDatePriceRangeAndOnlyOpenTrips()
    {
        var vm = Create();
        vm.Origin = "Hà Nội";
        vm.MinPrice = 100000;
        vm.MaxPrice = 300000;

        await vm.SearchCommand.ExecuteAsync(null);

        var criteria = _statistics.LastCriteria!;
        Assert.Equal(new DateTime(2026, 10, 17), criteria.From);
        Assert.Equal(new DateTime(2026, 10, 18), criteria.To);
        Assert.True(criteria.OnlyOpenForSale);
        Assert.Equal(100000, criteria.MinPrice);
        Assert.Equal(300000, criteria.MaxPrice);
        Assert.Equal("Hà Nội", criteria.Origin);
        Assert.Single(vm.Trips);
    }

    [Fact]
    public async Task SelectingTrip_BuildsSeatMapWithBookedSeats()
    {
        var vm = await CreateWithTripSelectedAsync();

        Assert.Equal(4, vm.SeatRows.Count);
        Assert.Equal(new[] { 1, 2 }, vm.SeatRows[0].Left.Select(s => s.Number));
        Assert.Equal(new[] { 3, 4 }, vm.SeatRows[0].Right.Select(s => s.Number));
        Assert.True(vm.SeatRows[0].Left[0].IsBooked);
        Assert.False(vm.SeatRows[0].Right[0].IsBooked);
    }

    [Fact]
    public async Task SelectSeat_IgnoresBookedSeats_AndKeepsSingleSelection()
    {
        var vm = await CreateWithTripSelectedAsync();
        var booked = vm.SeatRows[0].Left[0];
        var seat3 = vm.SeatRows[0].Right[0];
        var seat4 = vm.SeatRows[0].Right[1];

        vm.SelectSeatCommand.Execute(booked);
        Assert.Null(vm.SelectedSeat);

        vm.SelectSeatCommand.Execute(seat3);
        vm.SelectSeatCommand.Execute(seat4);
        Assert.Same(seat4, vm.SelectedSeat);
        Assert.False(seat3.IsSelected);
        Assert.True(seat4.IsSelected);
    }

    [Fact]
    public async Task LookupPassenger_FillsReturningPassenger()
    {
        _passengers.Items.Add(TestData.Passenger(1, "Nguyễn Thị Hoa", "0987000001"));
        var vm = Create();
        vm.Phone = "0987000001";

        await vm.LookupPassengerCommand.ExecuteAsync(null);

        Assert.Equal("Nguyễn Thị Hoa", vm.FullName);
        Assert.Equal("Sell_FoundPassenger|Nguyễn Thị Hoa", vm.PassengerHint);
    }

    [Fact]
    public async Task Book_PaysAndPrints_WhenUserAgrees()
    {
        _context.Dialogs.PaymentResult = PaymentMethod.Transfer;
        _context.Dialogs.ConfirmResult = true;
        _tickets.PrintModel = TestData.PrintModel(100);
        var vm = await CreateWithTripSelectedAsync();
        vm.SelectSeatCommand.Execute(vm.SeatRows[1].Left[0]);
        vm.Phone = "0987123456";
        vm.FullName = "Lê Văn Khách";
        Assert.True(vm.BookCommand.CanExecute(null));

        await vm.BookCommand.ExecuteAsync(null);

        Assert.Equal(new BookingRequest(7, "Lê Văn Khách", "0987123456", null, 5), _tickets.LastBooking);
        Assert.Equal((100L, PaymentMethod.Transfer), Assert.Single(_tickets.Payments));
        Assert.Equal(100, Assert.Single(_printer.Printed).TicketId);
        Assert.Equal(new[] { "Sell_Booked|100,5", "Sell_Paid|100" }, _context.Notifications.Successes);
        Assert.Equal(string.Empty, vm.Phone);
        Assert.True(vm.SeatRows[1].Left[0].IsBooked);
    }

    [Fact]
    public async Task Book_WithoutPayment_LeavesTicketUnpaidAndUnprinted()
    {
        _context.Dialogs.PaymentResult = null;
        _context.Dialogs.ConfirmResult = false;
        var vm = await CreateWithTripSelectedAsync();
        vm.SelectSeatCommand.Execute(vm.SeatRows[1].Left[0]);
        vm.Phone = "0987123456";
        vm.FullName = "Lê Văn Khách";

        await vm.BookCommand.ExecuteAsync(null);

        Assert.NotNull(_tickets.LastBooking);
        Assert.Empty(_tickets.Payments);
        Assert.Empty(_printer.Printed);
    }

    [Fact]
    public async Task Book_SeatTakenMeanwhile_ShowsSeatNumberInError()
    {
        _tickets.BookError = new BusinessRuleException(BusinessErrorCode.SeatAlreadyBooked, "Seat 5 is already booked.");
        var vm = await CreateWithTripSelectedAsync();
        vm.SelectSeatCommand.Execute(vm.SeatRows[1].Left[0]);
        vm.Phone = "0987123456";
        vm.FullName = "Lê Văn Khách";

        await vm.BookCommand.ExecuteAsync(null);

        Assert.Equal(new[] { "Err_20003|5" }, _context.Notifications.Errors);
    }
}
