using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Application.Abstractions;
using Transport.Domain.Errors;
using Transport.Presentation.Wpf.Localization;
using Transport.Presentation.Wpf.Services;

namespace Transport.Presentation.Wpf.ViewModels;

// Cửa sổ chính: menu theo quyền, điều hướng trang, đổi ngôn ngữ, đăng xuất
public sealed partial class MainViewModel : ObservableObject
{
    private readonly IUserSession _session;
    private readonly ILocalizationService _localization;
    private readonly IAuthService _auth;
    private readonly IDialogService _dialogs;
    private readonly IWindowService _windows;

    public MainViewModel(
        INavigationService navigation,
        NotificationService notifications,
        IUserSession session,
        ILocalizationService localization,
        IAuthService auth,
        IDialogService dialogs,
        IWindowService windows,
        IEnumerable<PageDescriptor> pages)
    {
        Navigation = navigation;
        Notifications = notifications;
        _session = session;
        _localization = localization;
        _auth = auth;
        _dialogs = dialogs;
        _windows = windows;
        Menu = [.. pages.Where(p => !p.OperatorOnly || session.IsOperator).OrderBy(p => p.Order)];
        SelectedMenu = Menu.FirstOrDefault();
    }

    public INavigationService Navigation { get; }

    public NotificationService Notifications { get; }

    public string UserFullName => _session.FullName;

    public string RoleKey => _session.IsOperator ? "Role_Operator" : "Role_TicketSeller";

    public string PageTitle => SelectedMenu is null ? string.Empty : _localization[SelectedMenu.TitleKey];

    [ObservableProperty]
    public partial IReadOnlyList<PageDescriptor> Menu { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageTitle))]
    public partial PageDescriptor? SelectedMenu { get; set; }

    partial void OnSelectedMenuChanged(PageDescriptor? value)
    {
        if (value is not null)
        {
            _ = Navigation.NavigateToAsync(value.ViewModelType);
        }
    }

    // Đổi ngôn ngữ rồi dựng lại menu + trang hiện tại để mọi chữ được dịch lại
    [RelayCommand]
    private void ToggleLanguage()
    {
        _localization.SetLanguage(_localization.CurrentLanguage == "vi" ? "en" : "vi");
        var current = SelectedMenu;
        Menu = [.. Menu];
        SelectedMenu = null;
        SelectedMenu = current;
        OnPropertyChanged(nameof(RoleKey));
    }

    [RelayCommand]
    private void Logout()
    {
        if (!_dialogs.Confirm(_localization["Msg_ConfirmLogout"]))
        {
            return;
        }

        _auth.Logout();
        _windows.ShowLogin();
    }

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        if (_dialogs.PromptPasswordChange(requireCurrent: true) is not { } change)
        {
            return;
        }

        try
        {
            await _auth.ChangePasswordAsync(change.CurrentPassword, change.NewPassword);
            Notifications.ShowSuccess(_localization["ChangePassword_Done"]);
        }
        catch (BusinessRuleException ex)
        {
            Notifications.ShowError(_localization.Translate(ex));
        }
    }
}
