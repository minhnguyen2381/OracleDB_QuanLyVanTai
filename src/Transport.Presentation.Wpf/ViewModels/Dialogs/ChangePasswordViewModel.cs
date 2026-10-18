using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Transport.Presentation.Wpf.ViewModels.Dialogs;

// Kết quả hộp thoại đổi mật khẩu; CurrentPassword rỗng khi bị bắt đổi lúc đăng nhập
public sealed record PasswordChange(string CurrentPassword, string NewPassword);

// Tự đổi mật khẩu (cần mật khẩu hiện tại) hoặc bắt buộc đặt mật khẩu mới khi mật khẩu hết hạn
public sealed partial class ChangePasswordViewModel : ObservableObject, IDialogViewModel
{
    public ChangePasswordViewModel(string title, bool requireCurrent)
    {
        Title = title;
        RequireCurrent = requireCurrent;
        CurrentPassword = string.Empty;
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
    }

    public event Action<bool>? CloseRequested;

    public string Title { get; }

    public bool RequireCurrent { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OkCommand))]
    public partial string CurrentPassword { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OkCommand))]
    [NotifyPropertyChangedFor(nameof(IsMismatch))]
    public partial string NewPassword { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OkCommand))]
    [NotifyPropertyChangedFor(nameof(IsMismatch))]
    public partial string ConfirmPassword { get; set; }

    // Đã nhập lại nhưng chưa khớp: hiện cảnh báo
    public bool IsMismatch => ConfirmPassword.Length > 0 && ConfirmPassword != NewPassword;

    public PasswordChange Result => new(CurrentPassword, NewPassword);

    [RelayCommand(CanExecute = nameof(CanOk))]
    private void Ok() => CloseRequested?.Invoke(true);

    private bool CanOk() =>
        (!RequireCurrent || CurrentPassword.Length > 0) && NewPassword.Length > 0 && NewPassword == ConfirmPassword;
}
