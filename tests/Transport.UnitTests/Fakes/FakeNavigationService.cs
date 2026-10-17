using CommunityToolkit.Mvvm.ComponentModel;
using Transport.Presentation.Wpf.Services;
using Transport.Presentation.Wpf.ViewModels;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeNavigationService : ObservableObject, INavigationService
{
    public List<Type> Navigated { get; } = [];

    public PageViewModel? CurrentPage => null;

    public Task NavigateToAsync(Type viewModelType)
    {
        Navigated.Add(viewModelType);
        return Task.CompletedTask;
    }
}
