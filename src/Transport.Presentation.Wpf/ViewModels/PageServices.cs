using Transport.Application.Abstractions;
using Transport.Presentation.Wpf.Localization;
using Transport.Presentation.Wpf.Services;

namespace Transport.Presentation.Wpf.ViewModels;

// Các dịch vụ giao diện dùng chung, gom lại để constructor của trang gọn
public sealed record PageServices(
    ILocalizationService Localization,
    INotificationService Notifications,
    IDialogService Dialogs,
    IUserSession Session,
    IExcelExporter Exporter);
