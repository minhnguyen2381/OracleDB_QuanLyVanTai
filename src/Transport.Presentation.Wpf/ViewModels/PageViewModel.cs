using CommunityToolkit.Mvvm.ComponentModel;
using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Errors;
using Transport.Presentation.Wpf.Localization;
using Transport.Presentation.Wpf.Services;

namespace Transport.Presentation.Wpf.ViewModels;

// Lớp nền của mọi trang trong cửa sổ chính
public abstract partial class PageViewModel : ObservableValidator
{
    protected PageViewModel(PageServices services) => Services = services;

    public bool IsOperator => Session.IsOperator;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    protected PageServices Services { get; }

    protected ILocalizationService Loc => Services.Localization;

    protected INotificationService Notify => Services.Notifications;

    protected IDialogService Dialogs => Services.Dialogs;

    protected IUserSession Session => Services.Session;

    // Gọi mỗi lần điều hướng tới trang
    public virtual Task LoadAsync() => Task.CompletedTask;

    // Chạy thao tác với trạng thái "đang xử lý"; lỗi nghiệp vụ được dịch và hiện ở thanh thông báo
    protected async Task RunBusyAsync(Func<Task> action, params object[] errorArgs)
    {
        IsBusy = true;
        try
        {
            await action();
        }
        catch (BusinessRuleException ex)
        {
            Notify.ShowError(Loc.Translate(ex, errorArgs));
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Xuất danh sách ra Excel; tiêu đề và tên cột là khóa resource
    protected Task ExportAsync<T>(IEnumerable<T> rows, IReadOnlyList<ExportColumn<T>> columns, string titleKey) =>
        RunBusyAsync(async () =>
        {
            var title = Loc[titleKey];
            var path = Dialogs.ShowSaveFile(title);
            if (path is null)
            {
                return;
            }

            var localized = columns.Select(c => c with { Header = Loc[c.Header] }).ToList();
            await Services.Exporter.ExportAsync(rows, localized, title, path);
            Notify.ShowSuccess(Loc.Format("Msg_Exported", path));
        });

    // Lỗi kiểm tra dữ liệu đầu tiên (đã dịch), dùng sau ValidateAllProperties()
    protected string FirstValidationError() =>
        GetErrors().Select(e => e.ErrorMessage).FirstOrDefault(m => m is not null) is { } key
            ? Loc[key]
            : Loc["Msg_FixErrors"];
}
