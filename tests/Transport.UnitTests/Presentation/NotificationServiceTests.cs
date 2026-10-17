using Transport.Presentation.Wpf.Services;

namespace Transport.UnitTests.Presentation;

public class NotificationServiceTests
{
    [Fact]
    public void ShowError_OpensBarWithMessageAndSeverity()
    {
        var notifications = new NotificationService();

        notifications.ShowError("Ghế 5 đã có người đặt.");

        Assert.True(notifications.IsOpen);
        Assert.Equal("Ghế 5 đã có người đặt.", notifications.Message);
        Assert.Equal(NotificationSeverity.Error, notifications.Severity);
    }

    [Fact]
    public void CloseCommand_HidesBar_AndNewMessageReplacesOld()
    {
        var notifications = new NotificationService();
        notifications.ShowSuccess("A");

        notifications.CloseCommand.Execute(null);
        Assert.False(notifications.IsOpen);

        notifications.ShowWarning("B");
        Assert.True(notifications.IsOpen);
        Assert.Equal("B", notifications.Message);
        Assert.Equal(NotificationSeverity.Warning, notifications.Severity);
    }
}
