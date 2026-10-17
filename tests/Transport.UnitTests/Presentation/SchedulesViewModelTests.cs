using Transport.Application.Services;
using Transport.Domain.Enums;
using Transport.Presentation.Wpf.ViewModels.Pages;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class SchedulesViewModelTests
{
    private static readonly FixedTimeProvider Time = new(new DateTimeOffset(2026, 10, 17, 8, 0, 0, TimeSpan.Zero));
    private readonly FakeStatisticsQueries _statistics = new();
    private readonly FakeScheduleRepository _scheduleRepository = new();
    private readonly FakeRouteRepository _routes = new();
    private readonly FakeVehicleRepository _vehicles = new();
    private readonly FakeDriverRepository _drivers = new();

    public SchedulesViewModelTests()
    {
        _routes.Items.Add(TestData.Route(1, "Hà Nội - Lào Cai", 280000, 330));
        _vehicles.Items.Add(TestData.Vehicle(1, "29B-196.88"));
        _vehicles.Items.Add(TestData.Vehicle(2, "35B-888.99", status: VehicleStatus.Maintenance));
        _drivers.Items.Add(TestData.Driver(1, "Nguyễn Văn An", new DateTime(2030, 1, 1)));
        _drivers.Items.Add(TestData.Driver(2, "Phạm Văn Dũng", new DateTime(2026, 10, 18)));
        _statistics.Trips.Add(FakeStatisticsQueries.Trip(5, departure: new DateTime(2026, 10, 18, 7, 0, 0)));
    }

    private SchedulesViewModel Create(PageTestContext context) => new(context.Services, _statistics,
        new ScheduleService(_scheduleRepository, context.Session), _routes, _vehicles, _drivers, Time);

    [Fact]
    public async Task Operator_LoadsTripsForNextWeekAndActiveVehiclesOnly()
    {
        var vm = Create(new PageTestContext());

        await vm.LoadAsync();

        Assert.Single(vm.Items);
        Assert.Equal("29B-196.88", Assert.Single(vm.Vehicles).PlateNumber);
        Assert.Equal(new DateTime(2026, 10, 17), _statistics.LastCriteria!.From);
        Assert.Equal(new DateTime(2026, 10, 25), _statistics.LastCriteria.To);
    }

    [Fact]
    public async Task Seller_SeesTripsButNoLookupsOrActions()
    {
        var vm = Create(PageTestContext.ForSeller());

        await vm.LoadAsync();
        vm.SelectedItem = vm.Items[0];

        Assert.Single(vm.Items);
        Assert.Equal(0, _routes.GetAllCount);
        Assert.False(vm.CanCreate);
        Assert.False(vm.CancelTripCommand.CanExecute(null));
    }

    [Fact]
    public async Task NewForm_ChoosingRouteSetsPriceAndArrival()
    {
        var vm = Create(new PageTestContext());
        await vm.LoadAsync();

        vm.NewCommand.Execute(null);
        vm.DepartureTimeText = "07:00";
        vm.SelectedRoute = vm.Routes[0];

        Assert.Equal(280000, vm.TicketPrice);
        Assert.Equal(new DateTime(2026, 10, 18), vm.ArrivalDate);
        Assert.Equal("12:30", vm.ArrivalTimeText);
    }

    [Fact]
    public async Task Drivers_AreFilteredByLicenceValidityOnDepartureDay()
    {
        var vm = Create(new PageTestContext());
        await vm.LoadAsync();
        vm.NewCommand.Execute(null);

        vm.DepartureDate = new DateTime(2026, 10, 18);
        Assert.Equal(2, vm.AvailableDrivers.Count);

        vm.DepartureDate = new DateTime(2026, 10, 20);
        Assert.Equal("Nguyễn Văn An", Assert.Single(vm.AvailableDrivers).FullName);
    }

    [Fact]
    public async Task Save_CombinesDateAndTime()
    {
        var vm = Create(new PageTestContext());
        await vm.LoadAsync();
        vm.NewCommand.Execute(null);
        vm.SelectedRoute = vm.Routes[0];
        vm.SelectedVehicle = vm.Vehicles[0];
        vm.DepartureDate = new DateTime(2026, 10, 20);
        vm.DepartureTimeText = "13:15";
        vm.SelectedDriver = vm.AvailableDrivers[0];

        await vm.SaveCommand.ExecuteAsync(null);

        var saved = _scheduleRepository.LastSchedule!;
        Assert.Equal(new DateTime(2026, 10, 20, 13, 15, 0), saved.DepartureTime);
        Assert.Equal(new DateTime(2026, 10, 20, 18, 45, 0), saved.ArrivalTime);
        Assert.Equal(280000, saved.TicketPrice);
        Assert.Equal(new[] { "add" }, _scheduleRepository.Calls);
    }

    [Fact]
    public async Task CancelTrip_PromptsForReason()
    {
        var context = new PageTestContext();
        var vm = Create(context);
        await vm.LoadAsync();
        vm.SelectedItem = vm.Items[0];

        context.Dialogs.PromptTextResult = null;
        await vm.CancelTripCommand.ExecuteAsync(null);
        Assert.Empty(_scheduleRepository.Calls);

        context.Dialogs.PromptTextResult = "Xe hỏng";
        await vm.CancelTripCommand.ExecuteAsync(null);
        Assert.Equal(new[] { "cancel:5:Xe hỏng" }, _scheduleRepository.Calls);
        Assert.Equal(new[] { "Schedules_Cancelled" }, context.Notifications.Successes);
    }
}
