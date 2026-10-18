using Transport.Domain.Errors;
using Transport.Presentation.Wpf.Services;
using Transport.Presentation.Wpf.ViewModels;
using Transport.Presentation.Wpf.ViewModels.Dialogs;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class MainViewModelTests
{
    private sealed class PageA;
    private sealed class PageB;
    private sealed class OperatorPage;

    private static readonly PageDescriptor[] Pages =
    [
        new("Menu_B", "\uE80F", typeof(PageB), OperatorOnly: false, Order: 20),
        new("Menu_Operator", "\uE80F", typeof(OperatorPage), OperatorOnly: true, Order: 30),
        new("Menu_A", "\uE80F", typeof(PageA), OperatorOnly: false, Order: 10),
    ];

    private readonly FakeNavigationService _navigation = new();
    private readonly FakeLocalizationService _localization = new();
    private readonly FakeAuthService _auth = new();
    private readonly FakeDialogService _dialogs = new();
    private readonly FakeWindowService _windows = new();

    private MainViewModel Create(FakeUserSession session) =>
        new(_navigation, new NotificationService(), session, _localization, _auth, _dialogs, _windows, Pages);

    [Fact]
    public void Menu_IsOrderedAndHidesOperatorPagesFromSellers()
    {
        var seller = Create(FakeUserSession.Seller());
        var operatorVm = Create(new FakeUserSession());

        Assert.Equal(new[] { "Menu_A", "Menu_B" }, seller.Menu.Select(p => p.TitleKey));
        Assert.Equal(new[] { "Menu_A", "Menu_B", "Menu_Operator" }, operatorVm.Menu.Select(p => p.TitleKey));
        Assert.Equal("Role_TicketSeller", seller.RoleKey);
        Assert.Equal("Role_Operator", operatorVm.RoleKey);
    }

    [Fact]
    public void Constructor_OpensFirstPage()
    {
        var main = Create(new FakeUserSession());

        Assert.Equal(new[] { typeof(PageA) }, _navigation.Navigated);
        Assert.Equal("Menu_A", main.PageTitle);
    }

    [Fact]
    public void SelectingMenuItem_Navigates()
    {
        var main = Create(new FakeUserSession());

        main.SelectedMenu = main.Menu[1];

        Assert.Equal(typeof(PageB), _navigation.Navigated[^1]);
        Assert.Equal("Menu_B", main.PageTitle);
    }

    [Fact]
    public void ToggleLanguage_SwitchesLanguageAndReloadsCurrentPage()
    {
        var main = Create(new FakeUserSession());

        main.ToggleLanguageCommand.Execute(null);

        Assert.Equal("en", _localization.CurrentLanguage);
        Assert.Equal(new[] { typeof(PageA), typeof(PageA) }, _navigation.Navigated);
        Assert.Equal("Menu_A", main.SelectedMenu!.TitleKey);
    }

    [Fact]
    public void Logout_WhenConfirmed_SignsOutAndShowsLogin()
    {
        var main = Create(new FakeUserSession());

        main.LogoutCommand.Execute(null);

        Assert.Equal(1, _auth.LogoutCount);
        Assert.Equal(new[] { "login" }, _windows.Shown);
    }

    [Fact]
    public void Logout_WhenCancelled_StaysSignedIn()
    {
        _dialogs.ConfirmResult = false;
        var main = Create(new FakeUserSession());

        main.LogoutCommand.Execute(null);

        Assert.Equal(0, _auth.LogoutCount);
        Assert.Empty(_windows.Shown);
    }

    [Fact]
    public async Task ChangePassword_Success_ShowsNotification()
    {
        _dialogs.PasswordChangeResult = new PasswordChange("Old#Pass1", "NewPass#2");
        var vm = Create(new FakeUserSession());

        await vm.ChangePasswordCommand.ExecuteAsync(null);

        Assert.Equal(new[] { true }, _dialogs.PasswordChangeRequests);
        Assert.Equal(("Old#Pass1", "NewPass#2"), _auth.LastChange);
        Assert.Equal("ChangePassword_Done", vm.Notifications.Message);
    }

    [Fact]
    public async Task ChangePassword_Failure_ShowsTranslatedError()
    {
        _dialogs.PasswordChangeResult = new PasswordChange("wrong", "NewPass#2");
        _auth.ChangeError = new BusinessRuleException(BusinessErrorCode.InvalidCredentials, "invalid");
        var vm = Create(new FakeUserSession());

        await vm.ChangePasswordCommand.ExecuteAsync(null);

        Assert.Equal("Err_1017", vm.Notifications.Message);
        Assert.Equal(NotificationSeverity.Error, vm.Notifications.Severity);
    }

    [Fact]
    public async Task ChangePassword_Cancelled_DoesNothing()
    {
        var vm = Create(new FakeUserSession());

        await vm.ChangePasswordCommand.ExecuteAsync(null);

        Assert.Null(_auth.LastChange);
    }
}
