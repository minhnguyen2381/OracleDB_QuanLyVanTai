using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Transport.Presentation.Wpf.ViewModels.Dialogs;

public sealed partial class PasswordPromptViewModel : ObservableObject, IDialogViewModel
{
    public PasswordPromptViewModel(string title, string label)
    {
        Title = title;
        Label = label;
        Password = string.Empty;
    }

    public event Action<bool>? CloseRequested;

    public string Title { get; }

    public string Label { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OkCommand))]
    public partial string Password { get; set; }

    [RelayCommand(CanExecute = nameof(CanOk))]
    private void Ok() => CloseRequested?.Invoke(true);

    private bool CanOk() => Password.Length > 0;
}
