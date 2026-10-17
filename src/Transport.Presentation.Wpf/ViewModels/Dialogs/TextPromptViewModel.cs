using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Transport.Presentation.Wpf.ViewModels.Dialogs;

public sealed partial class TextPromptViewModel : ObservableObject, IDialogViewModel
{
    public TextPromptViewModel(string title, string label)
    {
        Title = title;
        Label = label;
        Text = string.Empty;
    }

    public event Action<bool>? CloseRequested;

    public string Title { get; }

    public string Label { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OkCommand))]
    public partial string Text { get; set; }

    [RelayCommand(CanExecute = nameof(CanOk))]
    private void Ok() => CloseRequested?.Invoke(true);

    private bool CanOk() => !string.IsNullOrWhiteSpace(Text);
}
