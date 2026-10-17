using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Transport.Presentation.Wpf.Services;

public sealed partial class NotificationService : ObservableObject, INotificationService
{
    private CancellationTokenSource? _autoHide;

    public NotificationService() => Message = string.Empty;

    [ObservableProperty]
    public partial string Message { get; set; }

    [ObservableProperty]
    public partial NotificationSeverity Severity { get; set; }

    [ObservableProperty]
    public partial bool IsOpen { get; set; }

    public void ShowSuccess(string message) => Show(message, NotificationSeverity.Success, TimeSpan.FromSeconds(4));

    public void ShowWarning(string message) => Show(message, NotificationSeverity.Warning, TimeSpan.FromSeconds(6));

    public void ShowError(string message) => Show(message, NotificationSeverity.Error, TimeSpan.FromSeconds(8));

    [RelayCommand]
    private void Close()
    {
        _autoHide?.Cancel();
        IsOpen = false;
    }

    private void Show(string message, NotificationSeverity severity, TimeSpan duration)
    {
        _autoHide?.Cancel();
        _autoHide = new CancellationTokenSource();
        Message = message;
        Severity = severity;
        IsOpen = true;
        _ = HideLaterAsync(duration, _autoHide.Token);
    }

    // Tự ẩn sau một lúc; thông báo mới sẽ hủy lần ẩn của thông báo cũ
    private async Task HideLaterAsync(TimeSpan delay, CancellationToken token)
    {
        try
        {
            await Task.Delay(delay, token);
            IsOpen = false;
        }
        catch (OperationCanceledException)
        {
        }
    }
}
