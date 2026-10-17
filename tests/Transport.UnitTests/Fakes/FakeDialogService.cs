using Transport.Domain.Enums;
using Transport.Presentation.Wpf.Services;

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
}
