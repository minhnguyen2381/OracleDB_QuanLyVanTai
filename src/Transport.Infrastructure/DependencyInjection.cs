using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Transport.Application.Abstractions;
using Transport.Infrastructure.Persistence;
using Transport.Infrastructure.Repositories;

namespace Transport.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.AddSingleton<UserSession>();
        services.AddSingleton<IUserSession>(sp => sp.GetRequiredService<UserSession>());
        services.AddSingleton<OracleConnectionFactory>();
        services.AddSingleton<IAuthService, OracleAuthService>();

        services.AddSingleton<IRouteRepository, RouteRepository>();
        services.AddSingleton<IVehicleRepository, VehicleRepository>();
        services.AddSingleton<IDriverRepository, DriverRepository>();
        services.AddSingleton<IPassengerRepository, PassengerRepository>();
        services.AddSingleton<IScheduleRepository, ScheduleRepository>();
        services.AddSingleton<ITicketRepository, TicketRepository>();
        services.AddSingleton<IStaffRepository, StaffRepository>();
        services.AddSingleton<IStatisticsQueries, StatisticsQueries>();
        services.AddSingleton<IBackupRepository, BackupRepository>();
        return services;
    }
}
