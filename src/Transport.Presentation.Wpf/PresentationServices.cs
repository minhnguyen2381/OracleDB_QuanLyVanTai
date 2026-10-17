using Microsoft.Extensions.DependencyInjection;
using Transport.Presentation.Wpf.Localization;
using Transport.Presentation.Wpf.Services;
using Transport.Presentation.Wpf.ViewModels;
using Transport.Presentation.Wpf.ViewModels.Pages;
using Transport.Presentation.Wpf.Views;

namespace Transport.Presentation.Wpf;

public static class PresentationServices
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddSingleton<ILocalizationService>(LocalizationService.Instance);
        services.AddSingleton<NotificationService>();
        services.AddSingleton<INotificationService>(sp => sp.GetRequiredService<NotificationService>());
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IWindowService, WindowService>();
        services.AddSingleton<ITicketPrintService, TicketPrintService>();
        services.AddSingleton<PageServices>();
        // Mỗi cửa sổ chính có bộ điều hướng riêng
        services.AddTransient<INavigationService, NavigationService>();

        services.AddTransient<LoginViewModel>();
        services.AddTransient<LoginWindow>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<MainWindow>();
        return services.AddPages();
    }

    // Đăng ký một trang: ViewModel (mới mỗi lần mở) + mục menu
    public static IServiceCollection AddPage<TViewModel>(this IServiceCollection services, string titleKey, string glyph,
        int order, bool operatorOnly = false)
        where TViewModel : PageViewModel
    {
        services.AddTransient<TViewModel>();
        services.AddSingleton(new PageDescriptor(titleKey, glyph, typeof(TViewModel), operatorOnly, order));
        return services;
    }

    // Danh sách trang của ứng dụng (Plan 3 thêm từng dòng services.AddPage<...>(...) trước return)
    private static IServiceCollection AddPages(this IServiceCollection services)
    {
        services.AddPage<SellTicketViewModel>("Menu_SellTicket", "\uE7BF", 20);
        services.AddPage<TicketsViewModel>("Menu_Tickets", "\uE8A5", 30);
        services.AddPage<PassengersViewModel>("Menu_Passengers", "\uE716", 40);
        services.AddPage<RoutesViewModel>("Menu_Routes", "\uE707", 50);
        services.AddPage<VehiclesViewModel>("Menu_Vehicles", "\uE806", 60, operatorOnly: true);
        services.AddPage<DriversViewModel>("Menu_Drivers", "\uE77B", 70, operatorOnly: true);
        services.AddPage<SchedulesViewModel>("Menu_Schedules", "\uE787", 80);
        return services;
    }
}
