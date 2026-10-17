using System.Windows;
using Transport.Presentation.Wpf.ViewModels;

namespace Transport.Presentation.Wpf.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
