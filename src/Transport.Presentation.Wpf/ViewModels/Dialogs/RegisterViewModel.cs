using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Application.Services;
using Transport.Domain.Errors;
using Transport.Presentation.Wpf.Localization;

namespace Transport.Presentation.Wpf.ViewModels.Dialogs;

// Form đăng ký ở màn hình đăng nhập; lỗi hiện ngay trong hộp thoại để không phải nhập lại
public sealed partial class RegisterViewModel : ObservableObject, IDialogViewModel
{
    private readonly RegistrationService _registrations;
    private readonly ILocalizationService _localization;

    public RegisterViewModel(RegistrationService registrations, ILocalizationService localization)
    {
        _registrations = registrations;
        _localization = localization;
        Username = string.Empty;
        FullName = string.Empty;
        Phone = string.Empty;
    }

    public event Action<bool>? CloseRequested;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    public partial string Username { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    public partial string FullName { get; set; }

    [ObservableProperty]
    public partial string Phone { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        ErrorMessage = null;
        try
        {
            await _registrations.SubmitAsync(Username, FullName, Phone);
            CloseRequested?.Invoke(true);
        }
        catch (BusinessRuleException ex)
        {
            ErrorMessage = _localization.Translate(ex);
        }
    }

    private bool CanSubmit() => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(FullName);
}
