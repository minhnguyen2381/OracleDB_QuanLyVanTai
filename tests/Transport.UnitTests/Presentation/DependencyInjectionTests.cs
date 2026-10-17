using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Transport.Application;
using Transport.Infrastructure;
using Transport.Presentation.Wpf;
using Transport.Presentation.Wpf.Services;
using Transport.Presentation.Wpf.ViewModels;
using Transport.Presentation.Wpf.ViewModels.Pages;

namespace Transport.UnitTests.Presentation;

public class DependencyInjectionTests
{
    [Fact]
    public void ServiceProvider_ResolvesAllViewModelsAndPages()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddPresentation();

        using var provider = services.BuildServiceProvider();

        var descriptors = provider.GetServices<PageDescriptor>().ToList();
        Assert.Equal(11, descriptors.Count);

        foreach (var descriptor in descriptors)
        {
            var vm = provider.GetRequiredService(descriptor.ViewModelType);
            Assert.NotNull(vm);
            Assert.IsAssignableFrom<PageViewModel>(vm);
        }

        Assert.NotNull(provider.GetRequiredService<MainViewModel>());
        Assert.NotNull(provider.GetRequiredService<LoginViewModel>());
    }
}
