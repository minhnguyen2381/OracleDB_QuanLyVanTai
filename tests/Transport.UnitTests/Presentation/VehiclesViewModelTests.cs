using Transport.Domain.Enums;
using Transport.Presentation.Wpf.ViewModels.Pages;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class VehiclesViewModelTests
{
    private readonly PageTestContext _context = new();
    private readonly FakeVehicleRepository _vehicles = new();

    [Fact]
    public async Task New_DefaultsToActive_AndSaveUppercasesPlate()
    {
        var vm = new VehiclesViewModel(_context.Services, _vehicles);
        vm.NewCommand.Execute(null);
        Assert.Equal(VehicleStatus.Active, vm.Status);

        vm.PlateNumber = "30a-123.45";
        vm.VehicleType = "Limousine 16 chỗ";
        vm.SeatCount = 16;
        await vm.SaveCommand.ExecuteAsync(null);

        var added = Assert.Single(_vehicles.Added);
        Assert.Equal("30A-123.45", added.PlateNumber);
        Assert.Equal(16, added.SeatCount);
    }

    [Theory]
    [InlineData("29B19688", 40)]
    [InlineData("29B-196.88", 0)]
    [InlineData("29B-196.88", 61)]
    public async Task Save_RejectsInvalidPlateOrSeatCount(string plate, int seats)
    {
        var vm = new VehiclesViewModel(_context.Services, _vehicles);
        vm.NewCommand.Execute(null);
        vm.PlateNumber = plate;
        vm.VehicleType = "Ghế ngồi";
        vm.SeatCount = seats;

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Empty(_vehicles.Added);
        Assert.Single(_context.Notifications.Warnings);
    }

    [Fact]
    public async Task Edit_KeepsIdAndStatus()
    {
        _vehicles.Items.Add(TestData.Vehicle(3, status: VehicleStatus.Maintenance));
        var vm = new VehiclesViewModel(_context.Services, _vehicles);
        await vm.LoadAsync();
        vm.SelectedItem = vm.Items[0];

        vm.EditCommand.Execute(null);
        vm.Status = VehicleStatus.Active;
        await vm.SaveCommand.ExecuteAsync(null);

        var updated = Assert.Single(_vehicles.Updated);
        Assert.Equal(3, updated.Id);
        Assert.Equal(VehicleStatus.Active, updated.Status);
    }
}
