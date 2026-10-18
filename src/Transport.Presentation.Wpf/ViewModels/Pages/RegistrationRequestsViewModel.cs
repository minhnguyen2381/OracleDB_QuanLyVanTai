using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Application.Models;
using Transport.Application.Services;
using Transport.Domain.Enums;

namespace Transport.Presentation.Wpf.ViewModels.Pages;

// Duyệt/từ chối yêu cầu đăng ký tài khoản (PKG_REGISTRATION) – chỉ điều hành
public sealed partial class RegistrationRequestsViewModel : PageViewModel
{
    private readonly RegistrationService _registrations;
    private List<RegistrationRequest> _all = [];

    public RegistrationRequestsViewModel(PageServices services, RegistrationService registrations)
        : base(services)
    {
        _registrations = registrations;
        ShowOnlyPending = true;
    }

    public ObservableCollection<RegistrationRequest> Items { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApproveCommand))]
    [NotifyCanExecuteChangedFor(nameof(RejectCommand))]
    public partial RegistrationRequest? SelectedItem { get; set; }

    [ObservableProperty]
    public partial bool ShowOnlyPending { get; set; }

    public override Task LoadAsync() => RunBusyAsync(ReloadAsync);

    partial void OnShowOnlyPendingChanged(bool value) => ApplyFilter();

    [RelayCommand]
    private Task RefreshAsync() => RunBusyAsync(ReloadAsync);

    [RelayCommand(CanExecute = nameof(IsPendingSelected))]
    private Task ApproveAsync() => RunBusyAsync(async () =>
    {
        var request = SelectedItem!;
        if (Dialogs.PromptApproval(Loc.Format("Reg_ApproveTitle", request.Username)) is not { } input)
        {
            return;
        }

        await _registrations.ApproveAsync(request, input.TempPassword, input.Role);
        Notify.ShowSuccess(Loc["Reg_Approved"]);
        await ReloadAsync();
    });

    [RelayCommand(CanExecute = nameof(IsPendingSelected))]
    private Task RejectAsync() => RunBusyAsync(async () =>
    {
        var request = SelectedItem!;
        if (Dialogs.PromptText(Loc.Format("Reg_RejectTitle", request.Username), Loc["Reg_RejectReason"]) is not { } reason)
        {
            return;
        }

        await _registrations.RejectAsync(request.RequestId, reason);
        Notify.ShowSuccess(Loc["Reg_Rejected"]);
        await ReloadAsync();
    });

    [RelayCommand]
    private Task ExportToExcelAsync() => ExportAsync(Items.ToList(),
    [
        new("Reg_Id", r => r.RequestId),
        new("Staff_Username", r => r.Username),
        new("Staff_FullName", r => r.FullName),
        new("Staff_Phone", r => r.Phone),
        new("Reg_Status", r => Loc[$"RegStatus_{r.Status}"]),
        new("Reg_RequestedAt", r => r.RequestedAt, "dd/MM/yyyy HH:mm"),
        new("Reg_Host", r => r.ClientHost),
        new("Reg_ReviewedBy", r => r.ReviewedBy),
        new("Reg_ReviewedAt", r => r.ReviewedAt, "dd/MM/yyyy HH:mm"),
        new("Reg_RejectReason", r => r.RejectReason),
    ], "Reg_ExportTitle");

    private async Task ReloadAsync()
    {
        _all = [.. await _registrations.GetAllAsync()];
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Items.Clear();
        foreach (var request in _all.Where(r => !ShowOnlyPending || r.Status == RegistrationStatus.Pending))
        {
            Items.Add(request);
        }
    }

    private bool IsPendingSelected() => SelectedItem?.Status == RegistrationStatus.Pending;
}
