using CommunityToolkit.Mvvm.ComponentModel;

namespace Transport.Presentation.Wpf.ViewModels.Pages;

// Một ghế trên sơ đồ
public sealed partial class SeatViewModel : ObservableObject
{
    public SeatViewModel(int number, bool isBooked)
    {
        Number = number;
        IsBooked = isBooked;
    }

    public int Number { get; }

    public bool IsBooked { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
