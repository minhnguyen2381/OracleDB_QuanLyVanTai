using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Domain.Enums;

namespace Transport.Presentation.Wpf.ViewModels.Dialogs;

public sealed record ApprovalInput(string TempPassword, StaffRole Role);

// Duyệt yêu cầu đăng ký: đặt mật khẩu tạm và chọn role
public sealed partial class ApproveRegistrationViewModel : ObservableObject, IDialogViewModel
{
    public ApproveRegistrationViewModel(string title)
    {
        Title = title;
        TempPassword = string.Empty;
        Role = StaffRole.TicketSeller;
    }

    public event Action<bool>? CloseRequested;

    public string Title { get; }

    public IReadOnlyList<StaffRole> Roles { get; } = Enum.GetValues<StaffRole>();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OkCommand))]
    public partial string TempPassword { get; set; }

    [ObservableProperty]
    public partial StaffRole Role { get; set; }

    public ApprovalInput Result => new(TempPassword, Role);

    [RelayCommand(CanExecute = nameof(CanOk))]
    private void Ok() => CloseRequested?.Invoke(true);

    private bool CanOk() => TempPassword.Length > 0;
}
