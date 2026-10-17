namespace Transport.Presentation.Wpf.Services;

// Thanh thông báo (InfoBar) dưới thanh tiêu đề của cửa sổ chính
public interface INotificationService
{
    void ShowSuccess(string message);
    void ShowWarning(string message);
    void ShowError(string message);
}
