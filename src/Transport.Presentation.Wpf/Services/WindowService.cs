using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Transport.Presentation.Wpf.Views;

namespace Transport.Presentation.Wpf.Services;

public sealed class WindowService(IServiceProvider services) : IWindowService
{
    public void ShowMain() => Swap(services.GetRequiredService<MainWindow>());

    public void ShowLogin() => Swap(services.GetRequiredService<LoginWindow>());

    // Mở cửa sổ mới trước rồi mới đóng cửa sổ cũ để app không tự thoát
    private static void Swap(Window next)
    {
        var app = System.Windows.Application.Current;
        var previous = app.Windows.OfType<Window>().Where(w => w != next).ToList();
        app.MainWindow = next;
        next.Show();
        foreach (var window in previous)
        {
            window.Close();
        }
    }
}
