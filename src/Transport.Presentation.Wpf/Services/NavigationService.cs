using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Transport.Presentation.Wpf.ViewModels;

namespace Transport.Presentation.Wpf.Services;

public sealed partial class NavigationService(IServiceProvider services) : ObservableObject, INavigationService
{
    [ObservableProperty]
    public partial PageViewModel? CurrentPage { get; set; }

    public async Task NavigateToAsync(Type viewModelType)
    {
        var page = (PageViewModel)services.GetRequiredService(viewModelType);
        CurrentPage = page;
        await page.LoadAsync();
    }
}
