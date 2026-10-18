using Transport.Application.Services;
using Transport.Domain.Enums;
using Transport.Domain.Errors;
using Transport.Presentation.Wpf.ViewModels.Dialogs;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class DialogViewModelTests
{
    [Fact]
    public void TextPrompt_OkNeedsTextAndClosesWithTrue()
    {
        var dialog = new TextPromptViewModel("Hủy vé", "Lý do");
        bool? result = null;
        dialog.CloseRequested += r => result = r;

        Assert.False(dialog.OkCommand.CanExecute(null));
        dialog.Text = "Khách đổi ý";
        Assert.True(dialog.OkCommand.CanExecute(null));
        dialog.OkCommand.Execute(null);

        Assert.True(result);
    }

    [Fact]
    public void Payment_DefaultsToCashAndListsAllMethods()
    {
        var dialog = new PaymentDialogViewModel(280000);

        Assert.Equal(PaymentMethod.Cash, dialog.SelectedMethod);
        Assert.Equal(Enum.GetValues<PaymentMethod>(), dialog.Methods);
        Assert.Equal(280000, dialog.Amount);
    }

    [Fact]
    public void PasswordPrompt_OkNeedsPassword()
    {
        var dialog = new PasswordPromptViewModel("Đặt lại mật khẩu", "Mật khẩu mới");

        Assert.False(dialog.OkCommand.CanExecute(null));
        dialog.Password = "NewPass#01";
        Assert.True(dialog.OkCommand.CanExecute(null));
    }

    [Fact]
    public void ChangePassword_OkNeedsCurrentAndMatchingNewPasswords()
    {
        var dialog = new ChangePasswordViewModel("Đổi mật khẩu", requireCurrent: true);

        dialog.NewPassword = "NewPass#1";
        dialog.ConfirmPassword = "NewPass#2";
        Assert.True(dialog.IsMismatch);
        Assert.False(dialog.OkCommand.CanExecute(null));

        dialog.ConfirmPassword = "NewPass#1";
        Assert.False(dialog.IsMismatch);
        Assert.False(dialog.OkCommand.CanExecute(null));

        dialog.CurrentPassword = "Old#Pass1";
        Assert.True(dialog.OkCommand.CanExecute(null));
        Assert.Equal(new PasswordChange("Old#Pass1", "NewPass#1"), dialog.Result);
    }

    [Fact]
    public void ChangePassword_ForcedChange_DoesNotAskForCurrentPassword()
    {
        var dialog = new ChangePasswordViewModel("Đặt mật khẩu mới", requireCurrent: false)
        {
            NewPassword = "NewPass#1",
            ConfirmPassword = "NewPass#1",
        };

        Assert.True(dialog.OkCommand.CanExecute(null));
    }

    [Fact]
    public async Task Register_SubmitsAndCloses()
    {
        var repository = new FakeRegistrationRepository();
        var dialog = new RegisterViewModel(new RegistrationService(repository), new FakeLocalizationService());
        bool? result = null;
        dialog.CloseRequested += r => result = r;

        Assert.False(dialog.SubmitCommand.CanExecute(null));
        dialog.Username = "newuser01";
        dialog.FullName = "Người Mới";
        await dialog.SubmitCommand.ExecuteAsync(null);

        Assert.True(result);
        Assert.Equal(new[] { "submit:newuser01:Người Mới:" }, repository.Calls);
    }

    [Fact]
    public async Task Register_ShowsTranslatedErrorAndStaysOpen()
    {
        var dialog = new RegisterViewModel(new RegistrationService(new FakeRegistrationRepository()),
            new FakeLocalizationService())
        {
            Username = "ab",
            FullName = "Người Mới",
        };
        bool? result = null;
        dialog.CloseRequested += r => result = r;

        await dialog.SubmitCommand.ExecuteAsync(null);

        Assert.Null(result);
        Assert.Equal($"Err_{(int)BusinessErrorCode.InvalidUsername}", dialog.ErrorMessage);
    }
}
