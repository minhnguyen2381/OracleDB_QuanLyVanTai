using Transport.Domain.Enums;
using Transport.Presentation.Wpf.Services;
using Transport.Presentation.Wpf.ViewModels.Dialogs;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeDialogService : IDialogService
{
    public bool ConfirmResult { get; set; } = true;
    public List<string> ConfirmMessages { get; } = [];
    public string? SaveFilePath { get; set; } = @"C:\temp\export.xlsx";
    public string? PromptTextResult { get; set; }
    public string? PromptPasswordResult { get; set; }
    public PaymentMethod? PaymentResult { get; set; }
    public List<string> Prompts { get; } = [];
    public bool RegistrationResult { get; set; }
    public int RegistrationCount { get; private set; }
    public PasswordChange? PasswordChangeResult { get; set; }
    public List<bool> PasswordChangeRequests { get; } = [];

    public bool Confirm(string message)
    {
        ConfirmMessages.Add(message);
        return ConfirmResult;
    }

    public string? ShowSaveFile(string suggestedName) => SaveFilePath;

    public string? PromptText(string title, string label)
    {
        Prompts.Add(title);
        return PromptTextResult;
    }

    public string? PromptPassword(string title)
    {
        Prompts.Add(title);
        return PromptPasswordResult;
    }

    public PaymentMethod? ChoosePaymentMethod(decimal amount) => PaymentResult;

    public bool ShowRegistration()
    {
        RegistrationCount++;
        return RegistrationResult;
    }

    public PasswordChange? PromptPasswordChange(bool requireCurrent)
    {
        PasswordChangeRequests.Add(requireCurrent);
        return PasswordChangeResult;
    }
}

