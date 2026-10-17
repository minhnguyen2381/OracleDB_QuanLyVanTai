using Transport.Domain.Errors;
using Transport.Presentation.Wpf.ViewModels;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class LoginViewModelTests
{
    private readonly FakeAuthService _auth = new();
    private readonly FakeWindowService _windows = new();
    private readonly FakeLocalizationService _localization = new();

    private LoginViewModel CreateViewModel() => new(_auth, _localization, _windows);

    [Fact]
    public void Login_IsDisabledUntilUsernameAndPasswordAreEntered()
    {
        var login = CreateViewModel();
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
    }

    [Fact]
    public void ChangingLanguage_UpdatesLocalization()
    {
        var login = CreateViewModel();

        login.Language = "en";

        Assert.Equal("en", _localization.CurrentLanguage);
    }
}
