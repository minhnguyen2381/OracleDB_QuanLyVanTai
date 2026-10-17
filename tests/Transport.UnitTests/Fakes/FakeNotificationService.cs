using Transport.Presentation.Wpf.Services;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeNotificationService : INotificationService
{
    public List<string> Successes { get; } = [];
    public List<string> Warnings { get; } = [];
    public List<string> Errors { get; } = [];

    public void ShowSuccess(string message) => Successes.Add(message);
    public void ShowWarning(string message) => Warnings.Add(message);
    public void ShowError(string message) => Errors.Add(message);
}
