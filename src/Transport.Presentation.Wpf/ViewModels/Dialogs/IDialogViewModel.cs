namespace Transport.Presentation.Wpf.ViewModels.Dialogs;

// ViewModel của hộp thoại báo đóng (true = đồng ý)
public interface IDialogViewModel
{
    event Action<bool>? CloseRequested;
}
