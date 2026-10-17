using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Transport.Application;
using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Entities;
using Transport.Domain.Enums;
using Transport.Domain.Errors;
using Transport.Infrastructure;

namespace Transport.UnitTests.Infrastructure;

// Chạy với Oracle thật đã cài bằng Plan 1. Bật bằng biến môi trường TRANSPORT_DB_TESTS=1.
public class OracleIntegrationTests
{
    private static void RequireDatabase() =>
        Assert.SkipUnless(Environment.GetEnvironmentVariable("TRANSPORT_DB_TESTS") == "1",
            "Set TRANSPORT_DB_TESTS=1 to run Oracle integration tests.");

    private static ServiceProvider CreateServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Database:DataSource"] = "localhost:1521/XEPDB1" })
            .Build();
        return new ServiceCollection().AddApplication().AddInfrastructure(configuration).BuildServiceProvider();
    }

    private static async Task<ServiceProvider> SignInAsync(string username, string password)
    {
        var services = CreateServices();
        await services.GetRequiredService<IAuthService>().LoginAsync(username, password);
        return services;
    }

    private static Task<IReadOnlyList<TripStatistic>> OpenTripsAsync(ServiceProvider services) =>
        services.GetRequiredService<IStatisticsQueries>().GetTripsAsync(new TripSearchCriteria
        {
            From = DateTime.Today,
            To = DateTime.Today.AddDays(8),
            OnlyOpenForSale = true,
        });

    [Fact]
    public async Task Operator_ReadsSeededCatalogs()
    {
        RequireDatabase();
        using var services = await SignInAsync("operator01", "Operator#01");

        Assert.True(services.GetRequiredService<IUserSession>().IsOperator);
        Assert.Equal(5, (await services.GetRequiredService<IRouteRepository>().GetAllAsync()).Count);
        Assert.Equal(6, (await services.GetRequiredService<IVehicleRepository>().GetAllAsync()).Count);
        Assert.Equal(6, (await services.GetRequiredService<IDriverRepository>().GetAllAsync()).Count);
        Assert.True((await services.GetRequiredService<IPassengerRepository>().GetAllAsync()).Count >= 30);
        Assert.True((await services.GetRequiredService<IStaffRepository>().GetAllAsync()).Count >= 3);
        Assert.NotEmpty(await services.GetRequiredService<IStatisticsQueries>()
            .GetDailyRevenueAsync(DateTime.Today.AddDays(-7), DateTime.Today));
        Assert.True((await services.GetRequiredService<IBackupRepository>().GetJobStatusAsync())!.IsEnabled);
    }

    [Fact]
    public async Task Seller_SeesOpenTripsAndBookedSeats()
    {
        RequireDatabase();
        using var services = await SignInAsync("seller01", "Seller#001");

        var trips = await OpenTripsAsync(services);
        var seats = await services.GetRequiredService<ITicketRepository>().GetBookedSeatsAsync(trips[0].ScheduleId);

        Assert.False(services.GetRequiredService<IUserSession>().IsOperator);
        Assert.Equal("Trần Thị Bán", services.GetRequiredService<IUserSession>().FullName);
        Assert.All(seats, seat => Assert.InRange(seat, 1, trips[0].SeatCount));
        Assert.NotEmpty(await services.GetRequiredService<ITicketRepository>()
            .SearchAsync(new TicketSearchCriteria { From = DateTime.Today.AddDays(-8), To = DateTime.Today }));
    }

    [Fact]
    public async Task BookingSeatBeyondCapacity_IsMappedToSeatNotFound()
    {
        RequireDatabase();
        using var services = await SignInAsync("seller01", "Seller#001");
        var trip = (await OpenTripsAsync(services))[0];

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => services.GetRequiredService<ITicketRepository>()
            .BookAsync(new BookingRequest(trip.ScheduleId, "Integration Test", "0999111222", null, 99)));

        Assert.Equal(BusinessErrorCode.SeatNotFound, ex.Code);
    }

    [Fact]
    public async Task Operator_AddsAndDeletesPassenger()
    {
        RequireDatabase();
        using var services = await SignInAsync("operator01", "Operator#01");
        var repo = services.GetRequiredService<IPassengerRepository>();
        var p = new Passenger
        {
            FullName = "Nguyễn Văn Test",
            Phone = "0987654321",
            IdCard = "001099999999",
            Email = "test@example.com"
        };
        await repo.AddAsync(p);
        var found = await repo.FindByPhoneAsync("0987654321");
        Assert.NotNull(found);
        Assert.Equal("Nguyễn Văn Test", found.FullName);
        await repo.DeleteAsync(found.Id);
    }

    [Fact]
    public async Task Seller_BooksPaysAndCancelsTicket()
    {
        RequireDatabase();
        using var services = await SignInAsync("seller01", "Seller#001");
        var trips = await OpenTripsAsync(services);
        var trip = trips[0];
        var ticketRepo = services.GetRequiredService<ITicketRepository>();
        var bookedSeats = await ticketRepo.GetBookedSeatsAsync(trip.ScheduleId);
        var freeSeat = Enumerable.Range(1, trip.SeatCount).First(s => !bookedSeats.Contains(s));

        var ticketId = await ticketRepo.BookAsync(new BookingRequest(trip.ScheduleId, "Khách Test", "0912345678", null, freeSeat));
        Assert.True(ticketId > 0);

        await ticketRepo.PayAsync(ticketId, PaymentMethod.Cash);

        await ticketRepo.CancelAsync(ticketId, "Test hủy vé");
    }

    [Fact]
    public async Task UnknownUser_IsMappedToInvalidCredentials()
    {
        RequireDatabase();
        // Dùng user không tồn tại để không làm khóa tài khoản demo do nhập sai nhiều lần
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => SignInAsync("nobody_xyz", "wrong-password"));

        Assert.Equal(BusinessErrorCode.InvalidCredentials, ex.Code);
    }
}
