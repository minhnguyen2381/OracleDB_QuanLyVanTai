using Microsoft.Extensions.DependencyInjection;
using Transport.Presentation.Wpf.Services;
using Transport.Presentation.Wpf.ViewModels;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class NavigationServiceTests
{
    internal sealed class LoadingPage(PageServices services) : PageViewModel(services)
    {
        public bool Loaded { get; private set; }

        public override Task LoadAsync()
        {
            Loaded = true;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task NavigateToAsync_CreatesPageFromContainerAndLoadsIt()
    {
        var provider = new ServiceCollection()
            .AddSingleton(new PageTestContext().Services)
            .AddTransient<LoadingPage>()
            .BuildServiceProvider();
        var navigation = new NavigationService(provider);

        await navigation.NavigateToAsync(typeof(LoadingPage));

        var page = Assert.IsType<LoadingPage>(navigation.CurrentPage);
        Assert.True(page.Loaded);
    }
}
