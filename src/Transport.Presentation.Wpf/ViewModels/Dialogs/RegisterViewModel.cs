using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Application.Services;
using Transport.Domain.Errors;
using Transport.Presentation.Wpf.Localization;

namespace Transport.Presentation.Wpf.ViewModels.Dialogs;

// Form tự đăng ký ở màn hình đăng nhập; lỗi hiện ngay trong hộp thoại để không phải nhập lại
public sealed partial class RegisterViewModel : ObservableObject, IDialogViewModel
{
    private readonly RegistrationService _registrations;
    private readonly ILocalizationService _localization;

    public RegisterViewModel(RegistrationService registrations, ILocalizationService localization)
    {
        _registrations = registrations;
        _localization = localization;
        Username = string.Empty;
        Password = string.Empty;
        ConfirmPassword = string.Empty;
        FullName = string.Empty;
        Phone = string.Empty;
    }

    public event Action<bool>? CloseRequested;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    public partial string Username { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    [NotifyPropertyChangedFor(nameof(IsMismatch))]
    public partial string Password { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    [NotifyPropertyChangedFor(nameof(IsMismatch))]
    public partial string ConfirmPassword { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    public partial string FullName { get; set; }

    [ObservableProperty]
    public partial string Phone { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    // Đã nhập lại nhưng chưa khớp: hiện cảnh báo
    public bool IsMismatch => ConfirmPassword.Length > 0 && ConfirmPassword != Password;

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        ErrorMessage = null;
        try
        {
            await _registrations.RegisterAsync(Username, Password, FullName, Phone);
            CloseRequested?.Invoke(true);
        }
        catch (BusinessRuleException ex)
        {
            ErrorMessage = _localization.Translate(ex);
        }
    }

    private bool CanSubmit() =>
        !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(FullName)
        && Password.Length > 0 && Password == ConfirmPassword;
}
