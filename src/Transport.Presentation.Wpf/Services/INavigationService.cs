using System.ComponentModel;
using Transport.Presentation.Wpf.ViewModels;

namespace Transport.Presentation.Wpf.Services;

public interface INavigationService : INotifyPropertyChanged
{
    PageViewModel? CurrentPage { get; }

    // Tạo ViewModel trang mới từ DI rồi gọi LoadAsync
    Task NavigateToAsync(Type viewModelType);
}
