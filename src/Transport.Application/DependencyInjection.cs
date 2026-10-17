using Microsoft.Extensions.DependencyInjection;
using Transport.Application.Services;

namespace Transport.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<TicketService>();
        services.AddSingleton<ScheduleService>();
        services.AddSingleton<StaffService>();
        services.AddSingleton<BackupService>();
        services.AddSingleton<DashboardService>();
        return services;
    }
}
