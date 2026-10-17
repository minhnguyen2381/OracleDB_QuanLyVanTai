using Transport.Application.Services;
using Transport.Domain.Entities;
using Transport.Domain.Errors;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Application;

public class ScheduleServiceTests
{
    private readonly FakeScheduleRepository _repository = new();

    private static Schedule ValidSchedule() => new()
    {
        RouteId = 1,
        VehicleId = 2,
        DriverId = 3,
        DepartureTime = new DateTime(2026, 10, 20, 7, 0, 0),
        ArrivalTime = new DateTime(2026, 10, 20, 12, 30, 0),
        TicketPrice = 280000,
    };

    [Fact]
    public async Task AddAsync_SavesValidSchedule()
    {
        await new ScheduleService(_repository, new FakeUserSession()).AddAsync(ValidSchedule());

        Assert.Equal(new[] { "add" }, _repository.Calls);
    }

    [Fact]
    public async Task AddAsync_RejectsArrivalNotAfterDeparture()
    {
        var schedule = ValidSchedule() with { ArrivalTime = new DateTime(2026, 10, 20, 7, 0, 0) };

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => new ScheduleService(_repository, new FakeUserSession()).AddAsync(schedule));

        Assert.Equal(BusinessErrorCode.InvalidTimeRange, ex.Code);
        Assert.Empty(_repository.Calls);
    }

    [Fact]
    public async Task UpdateAsync_RejectsNonPositivePriceAndMissingDriver()
    {
        var service = new ScheduleService(_repository, new FakeUserSession());

        var price = await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateAsync(ValidSchedule() with { TicketPrice = 0 }));
        var driver = await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateAsync(ValidSchedule() with { DriverId = 0 }));

        Assert.Equal(BusinessErrorCode.InvalidPrice, price.Code);
        Assert.Equal(BusinessErrorCode.RequiredField, driver.Code);
    }

    [Fact]
    public async Task Seller_CannotChangeSchedules()
    {
        var service = new ScheduleService(_repository, FakeUserSession.Seller());

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CancelAsync(5, "Xe hỏng"));

        Assert.Equal(BusinessErrorCode.OperatorOnly, ex.Code);
    }

    [Fact]
    public async Task StatusAndCancelCallsReachRepository()
    {
        var service = new ScheduleService(_repository, new FakeUserSession());

        await service.MarkRunningAsync(5);
        await service.MarkCompletedAsync(5);
        await service.CancelAsync(6, " Xe hỏng ");
        await service.DeleteAsync(7);

        Assert.Equal(new[] { "status:5:Running", "status:5:Completed", "cancel:6:Xe hỏng", "delete:7" }, _repository.Calls);
    }
}
