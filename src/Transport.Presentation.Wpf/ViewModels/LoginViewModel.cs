using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Application.Abstractions;
using Transport.Domain.Errors;
using Transport.Presentation.Wpf.Localization;
using Transport.Presentation.Wpf.Services;

namespace Transport.Presentation.Wpf.ViewModels;

public sealed partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _auth;
    private readonly ILocalizationService _localization;
    private readonly IWindowService _windows;
    private readonly IDialogService _dialogs;

    public LoginViewModel(IAuthService auth, ILocalizationService localization, IWindowService windows, IDialogService dialogs)
    {
        _auth = auth;
        _localization = localization;
        _windows = windows;
        _dialogs = dialogs;
        Username = string.Empty;
        Password = string.Empty;
        Language = localization.CurrentLanguage;
    }

    public IReadOnlyList<string> Languages { get; } = ["vi", "en"];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    public partial string Username { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    public partial string Password { get; set; }

    [ObservableProperty]
    public partial string Language { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string? InfoMessage { get; set; }

    partial void OnLanguageChanged(string value) => _localization.SetLanguage(value);

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        ErrorMessage = null;
        InfoMessage = null;
        var username = Username?.Trim() ?? string.Empty;
        try
        {
            await _auth.LoginAsync(username, Password);
            _windows.ShowMain();
        }
        catch (BusinessRuleException ex) when (ex.Code == BusinessErrorCode.PasswordExpired)
        {
            await ChangeExpiredPasswordAsync(username);
        }
        catch (BusinessRuleException ex)
        {
            Fail(ex);
        }
    }

    [RelayCommand]
    private void Register()
    {
        if (_dialogs.ShowRegistration())
        {
            InfoMessage = _localization["Register_Submitted"];
        }
    }

    // Mật khẩu hết hạn hoặc là mật khẩu tạm do điều hành cấp: bắt đặt mật khẩu mới rồi mới vào app
    private async Task ChangeExpiredPasswordAsync(string username)
    {
        if (_dialogs.PromptPasswordChange(requireCurrent: false) is not { } change)
        {
            Password = string.Empty;
            return;
        }

        try
        {
            await _auth.LoginWithNewPasswordAsync(username, Password, change.NewPassword);
            _windows.ShowMain();
        }
        catch (BusinessRuleException ex)
        {
            Fail(ex);
        }
    }

    // Báo lỗi và xóa ô mật khẩu để không thử lại bằng mật khẩu cũ
    private void Fail(BusinessRuleException ex)
    {
        ErrorMessage = _localization.Translate(ex);
        Password = string.Empty;
    }

    private bool CanLogin() => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrEmpty(Password);
}
