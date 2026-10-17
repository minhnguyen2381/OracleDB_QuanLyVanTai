using Transport.Domain.Enums;
using Transport.Presentation.Wpf.ViewModels.Pages;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class DriversViewModelTests
{
    private static readonly FixedTimeProvider Time = new(new DateTimeOffset(2026, 10, 17, 8, 0, 0, TimeSpan.Zero));
    private readonly PageTestContext _context = new();
    private readonly FakeDriverRepository _drivers = new();

    [Fact]
    public async Task Load_CountsExpiringAndExpiredLicences()
    {
        _drivers.Items.Add(TestData.Driver(1, expiry: new DateTime(2028, 1, 1)));
        _drivers.Items.Add(TestData.Driver(2, expiry: new DateTime(2026, 11, 1)));
        _drivers.Items.Add(TestData.Driver(3, expiry: new DateTime(2026, 10, 1)));
        var vm = new DriversViewModel(_context.Services, _drivers, Time);

        await vm.LoadAsync();

        Assert.Equal("Drivers_ExpiryWarning|2", vm.ExpiryWarning);
    }

    [Fact]
    public async Task Load_NoWarningWhenAllValid()
    {
        _drivers.Items.Add(TestData.Driver(1, expiry: new DateTime(2028, 1, 1)));
        var vm = new DriversViewModel(_context.Services, _drivers, Time);

        await vm.LoadAsync();

        Assert.Null(vm.ExpiryWarning);
    }

    [Theory]
    [InlineData(31, false)]
    [InlineData(30, false)]
    [InlineData(29, true)]
    [InlineData(0, true)]
    [InlineData(-1, true)]
    public async Task Load_LicenceExpiryBoundaries(int daysFromToday, bool shouldWarn)
    {
        var drivers = new FakeDriverRepository();
        var baseDate = Time.GetLocalNow().Date;
        drivers.Items.Add(TestData.Driver(1, expiry: baseDate.AddDays(daysFromToday)));
        var vm = new DriversViewModel(new PageTestContext().Services, drivers, Time);

        await vm.LoadAsync();

        if (shouldWarn)
        {
            Assert.Equal("Drivers_ExpiryWarning|1", vm.ExpiryWarning);
        }
        else
        {
            Assert.Null(vm.ExpiryWarning);
        }
    }


    [Fact]
    public async Task Save_ValidatesIdCardAndPhone_ThenAdds()
    {
        var vm = new DriversViewModel(_context.Services, _drivers, Time);
        vm.NewCommand.Execute(null);
        Assert.Equal(new DateTime(2027, 10, 17), vm.LicenseExpiry);
        vm.FullName = "Đỗ Văn Giang";
        vm.IdCard = "12345";
        vm.Phone = "0912345699";
        vm.LicenseNo = "790999999999";

        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Empty(_drivers.Added);
        Assert.Equal(new[] { "Val_IdCard" }, _context.Notifications.Warnings);

        vm.IdCard = "001099012345";
        vm.LicenseClass = LicenseClass.D2;
        await vm.SaveCommand.ExecuteAsync(null);
        var added = Assert.Single(_drivers.Added);
        Assert.Equal(LicenseClass.D2, added.LicenseClass);
        Assert.Equal("Đỗ Văn Giang", added.FullName);
    }
}
