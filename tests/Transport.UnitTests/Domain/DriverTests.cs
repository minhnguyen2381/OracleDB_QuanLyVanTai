using Transport.Domain.Entities;
using Transport.Domain.Enums;

namespace Transport.UnitTests.Domain;

public class DriverTests
{
    private static readonly DateTime Today = new(2026, 10, 17);

    private static Driver CreateDriver(DateTime expiry, DriverStatus status = DriverStatus.Active) => new()
    {
        Id = 1,
        FullName = "Nguyễn Văn An",
        IdCard = "001085012345",
        Phone = "0912345601",
        LicenseNo = "790123456781",
        LicenseClass = LicenseClass.D,
        LicenseExpiry = expiry,
        Status = status,
    };

    [Theory]
    [InlineData(-1, LicenseState.Expired)]
    [InlineData(0, LicenseState.ExpiringSoon)]
    [InlineData(29, LicenseState.ExpiringSoon)]
    [InlineData(30, LicenseState.Valid)]
    public void GetLicenseState_UsesThirtyDayWarningWindow(int daysUntilExpiry, LicenseState expected)
    {
        var driver = CreateDriver(Today.AddDays(daysUntilExpiry));

        Assert.Equal(expected, driver.GetLicenseState(Today));
    }

    [Fact]
    public void CanDrive_RequiresActiveStatusAndLicenseValidOnDepartureDay()
    {
        var driver = CreateDriver(Today.AddDays(10));

        Assert.True(driver.CanDrive(Today.AddDays(10).AddHours(20)));
        Assert.False(driver.CanDrive(Today.AddDays(11)));
        Assert.False((driver with { Status = DriverStatus.Inactive }).CanDrive(Today));
    }
}
