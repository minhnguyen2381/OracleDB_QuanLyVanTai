using Transport.Domain.Enums;
using Transport.Presentation.Wpf.ViewModels.Dialogs;

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
}
