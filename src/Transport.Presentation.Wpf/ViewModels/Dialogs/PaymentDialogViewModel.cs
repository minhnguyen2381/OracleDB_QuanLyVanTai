using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Domain.Enums;

namespace Transport.Presentation.Wpf.ViewModels.Dialogs;

public sealed partial class PaymentDialogViewModel : ObservableObject, IDialogViewModel
{
    public PaymentDialogViewModel(decimal amount)
    {
        Amount = amount;
        SelectedMethod = PaymentMethod.Cash;
    }

    public event Action<bool>? CloseRequested;

    public decimal Amount { get; }

    public IReadOnlyList<PaymentMethod> Methods { get; } = Enum.GetValues<PaymentMethod>();

    [ObservableProperty]
    public partial PaymentMethod SelectedMethod { get; set; }

    [RelayCommand]
    private void Ok() => CloseRequested?.Invoke(true);
}
