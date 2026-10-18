using System.IO;
using System.Windows;
using Microsoft.Win32;
using Transport.Application.Services;
using Transport.Domain.Enums;
using Transport.Presentation.Wpf.Localization;
using Transport.Presentation.Wpf.ViewModels.Dialogs;
using Transport.Presentation.Wpf.Views.Dialogs;

namespace Transport.Presentation.Wpf.Services;

public sealed class DialogService(ILocalizationService localization, RegistrationService registrations) : IDialogService
{
    public bool Confirm(string message)
    {
        var owner = ActiveWindow();
        var title = localization["App_Title"];
        var result = owner is null
            ? MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question)
            : MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    }

    public string? ShowSaveFile(string suggestedName)
    {
        var dialog = new SaveFileDialog
        {
            FileName = $"{SafeFileName(suggestedName)}_{DateTime.Now:yyyyMMdd_HHmm}",
            DefaultExt = ".xlsx",
            AddExtension = true,
            Filter = "Excel (*.xlsx)|*.xlsx",
        };
        var owner = ActiveWindow();
        var accepted = owner is null ? dialog.ShowDialog() : dialog.ShowDialog(owner);
        return accepted == true ? dialog.FileName : null;
    }

    public string? PromptText(string title, string label)
    {
        var viewModel = new TextPromptViewModel(title, label);
        return ShowModal(new TextPromptDialog(), viewModel) ? viewModel.Text.Trim() : null;
    }

    public string? PromptPassword(string title)
    {
        var viewModel = new PasswordPromptViewModel(title, localization["Dialog_Password_Label"]);
        return ShowModal(new PasswordPromptDialog(), viewModel) ? viewModel.Password : null;
    }

    public PaymentMethod? ChoosePaymentMethod(decimal amount)
    {
        var viewModel = new PaymentDialogViewModel(amount);
        return ShowModal(new PaymentDialog(), viewModel) ? viewModel.SelectedMethod : null;
    }

    public bool ShowRegistration() => ShowModal(new RegisterDialog(), new RegisterViewModel(registrations, localization));

    public PasswordChange? PromptPasswordChange(bool requireCurrent)
    {
        var title = localization[requireCurrent ? "ChangePassword_Title" : "ChangePassword_ExpiredTitle"];
        var viewModel = new ChangePasswordViewModel(title, requireCurrent);
        return ShowModal(new ChangePasswordDialog(), viewModel) ? viewModel.Result : null;
    }

    private static bool ShowModal(Window dialog, IDialogViewModel viewModel)
    {
        dialog.DataContext = viewModel;
        dialog.Owner = ActiveWindow();
        viewModel.CloseRequested += result => dialog.DialogResult = result;
        return dialog.ShowDialog() == true;
    }

    private static Window? ActiveWindow()
    {
        var app = System.Windows.Application.Current;
        return app?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? app?.MainWindow;
    }

    private static string SafeFileName(string name) =>
        string.Concat(name.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
}
