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

    public LoginViewModel(IAuthService auth, ILocalizationService localization, IWindowService windows)
    {
        _auth = auth;
        _localization = localization;
        _windows = windows;
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

    partial void OnLanguageChanged(string value) => _localization.SetLanguage(value);

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        ErrorMessage = null;
        try
        {
            await _auth.LoginAsync(Username.Trim(), Password);
            _windows.ShowMain();
        }
        catch (BusinessRuleException ex)
        {
            ErrorMessage = _localization.Translate(ex);
        }
    }

    private bool CanLogin() => Username.Trim().Length > 0 && Password.Length > 0;
}
