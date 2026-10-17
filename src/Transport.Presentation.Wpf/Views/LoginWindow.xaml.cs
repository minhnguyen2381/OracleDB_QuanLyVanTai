using System.Windows;
using Transport.Presentation.Wpf.ViewModels;

namespace Transport.Presentation.Wpf.Views;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
