using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Transport.Application;
using Transport.Infrastructure;
using Transport.Presentation.Wpf.Localization;
using Transport.Presentation.Wpf.Services;

namespace Transport.Presentation.Wpf;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Số tiền, ngày giờ luôn theo định dạng Việt Nam (1.250.000 ₫, 17/10/2026) dù giao diện tiếng Anh
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage("vi-VN")));
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        // Composition root: Generic Host đọc appsettings.json và dựng DI cho cả 3 tầng
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = e.Args,
            ContentRootPath = AppContext.BaseDirectory,
        });
        builder.Services.AddApplication().AddInfrastructure(builder.Configuration).AddPresentation();
        _host = builder.Build();
        _host.Start();

        LocalizationService.Instance.SetLanguage(builder.Configuration["Language"] ?? "vi");
        _host.Services.GetRequiredService<IWindowService>().ShowLogin();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        base.OnExit(e);
    }

    // Lỗi ngoài dự kiến: báo cho người dùng thay vì tắt ứng dụng
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(e.Exception.Message, LocalizationService.Instance["App_Title"],
            MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
