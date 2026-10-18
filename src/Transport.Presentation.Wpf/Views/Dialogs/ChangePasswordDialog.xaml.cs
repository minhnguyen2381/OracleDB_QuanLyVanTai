using System.Windows;

namespace Transport.Presentation.Wpf.Views.Dialogs;

public partial class ChangePasswordDialog : Window
{
    public ChangePasswordDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => (CurrentBox.IsVisible ? CurrentBox : NewBox).Focus();
    }
}
