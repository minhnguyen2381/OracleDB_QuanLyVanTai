using Transport.Domain.Errors;
using Transport.Presentation.Wpf.ViewModels;
using Transport.Presentation.Wpf.ViewModels.Dialogs;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class LoginViewModelTests
{
    private readonly FakeAuthService _auth = new();
    private readonly FakeWindowService _windows = new();
    private readonly FakeLocalizationService _localization = new();
    private readonly FakeDialogService _dialogs = new();

    private LoginViewModel CreateViewModel() => new(_auth, _localization, _windows, _dialogs);

    [Fact]
    public void Login_IsDisabledUntilUsernameAndPasswordAreEntered()
    {
        var login = CreateViewModel();
        Assert.False(login.LoginCommand.CanExecute(null));

        login.Username = null!;
        Assert.False(login.LoginCommand.CanExecute(null));

        login.Username = "   ";
        Assert.False(login.LoginCommand.CanExecute(null));

        login.Username = "seller01";
        login.Password = "Seller#001";

        Assert.True(login.LoginCommand.CanExecute(null));
    }

    [Fact]
    public async Task Login_Success_OpensMainWindow()
    {
        var login = CreateViewModel();
        login.Username = "  seller01 ";
        login.Password = "Seller#001";

        await login.LoginCommand.ExecuteAsync(null);

        Assert.Equal(("seller01", "Seller#001"), _auth.LastLogin);
        Assert.Equal(new[] { "main" }, _windows.Shown);
        Assert.Null(login.ErrorMessage);
    }

    [Fact]
    public async Task Login_Failure_ShowsTranslatedError()
    {
        _auth.LoginError = new BusinessRuleException(BusinessErrorCode.InvalidCredentials, "invalid");
        var login = CreateViewModel();
        login.Username = "seller01";
        login.Password = "wrong";

        await login.LoginCommand.ExecuteAsync(null);

        Assert.Equal("Err_1017", login.ErrorMessage);
        Assert.Empty(_windows.Shown);
        Assert.Equal(string.Empty, login.Password);
    }

    [Fact]
    public async Task ExpiredPassword_AsksForNewPasswordThenSignsIn()
    {
        _auth.LoginError = new BusinessRuleException(BusinessErrorCode.PasswordExpired, "expired");
        _dialogs.PasswordChangeResult = new PasswordChange(string.Empty, "NewPass#2");
        var login = CreateViewModel();
        login.Username = " seller01 ";
        login.Password = "Seller#001";

        await login.LoginCommand.ExecuteAsync(null);

        Assert.Equal(new[] { false }, _dialogs.PasswordChangeRequests);
        Assert.Equal(("seller01", "Seller#001", "NewPass#2"), _auth.LastNewPasswordLogin);
        Assert.Equal(new[] { "main" }, _windows.Shown);
    }

    [Fact]
    public async Task ExpiredPassword_Cancelled_StaysOnLogin()
    {
        _auth.LoginError = new BusinessRuleException(BusinessErrorCode.PasswordExpired, "expired");
        var login = CreateViewModel();
        login.Username = "seller01";
        login.Password = "Seller#001";

        await login.LoginCommand.ExecuteAsync(null);

        Assert.Null(_auth.LastNewPasswordLogin);
        Assert.Empty(_windows.Shown);
        Assert.Equal(string.Empty, login.Password);
    }

    [Fact]
    public async Task ExpiredPassword_WeakNewPassword_ShowsError()
    {
        _auth.LoginError = new BusinessRuleException(BusinessErrorCode.PasswordExpired, "expired");
        _auth.NewPasswordError = new BusinessRuleException(BusinessErrorCode.InvalidPassword, "weak");
        _dialogs.PasswordChangeResult = new PasswordChange(string.Empty, "weak");
        var login = CreateViewModel();
        login.Username = "seller01";
        login.Password = "Seller#001";

        await login.LoginCommand.ExecuteAsync(null);

        Assert.Equal("Err_20044", login.ErrorMessage);
        Assert.Empty(_windows.Shown);
    }

    [Theory]
    [InlineData("newuser01", "Register_Submitted")]
    [InlineData(null, null)]
    public void Register_PrefillsUsernameOnlyWhenRegistered(string? registered, string? expected)
    {
        _dialogs.RegistrationResult = registered;
        var login = CreateViewModel();

        login.RegisterCommand.Execute(null);

        Assert.Equal(1, _dialogs.RegistrationCount);
        Assert.Equal(expected, login.InfoMessage);
        Assert.Equal(registered ?? string.Empty, login.Username);
    }

    [Fact]
    public void ChangingLanguage_UpdatesLocalization()
    {
        var login = CreateViewModel();

        login.Language = "en";

        Assert.Equal("en", _localization.CurrentLanguage);
    }
}
