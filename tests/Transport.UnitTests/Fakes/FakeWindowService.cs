using Transport.Presentation.Wpf.Services;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeWindowService : IWindowService
{
    public List<string> Shown { get; } = [];

    public void ShowMain() => Shown.Add("main");

    public void ShowLogin() => Shown.Add("login");
}
