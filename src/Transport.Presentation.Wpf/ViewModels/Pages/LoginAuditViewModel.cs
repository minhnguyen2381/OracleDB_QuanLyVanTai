using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Application.Abstractions;
using Transport.Application.Models;

namespace Transport.Presentation.Wpf.ViewModels.Pages;

// Nhật ký đăng nhập và thao tác quản trị tài khoản (V_LOGIN_AUDIT) – chỉ điều hành
public sealed partial class LoginAuditViewModel : PageViewModel
{
    private readonly ILoginAuditQueries _audit;

    public LoginAuditViewModel(PageServices services, ILoginAuditQueries audit, TimeProvider time)
        : base(services)
    {
        _audit = audit;
        var today = time.GetLocalNow().Date;
        FromDate = today.AddDays(-7);
        ToDate = today;
        UsernameFilter = string.Empty;
    }

    public ObservableCollection<LoginAuditEntry> Entries { get; } = [];

    [ObservableProperty]
    public partial string UsernameFilter { get; set; }

    [ObservableProperty]
    public partial DateTime FromDate { get; set; }

    [ObservableProperty]
    public partial DateTime ToDate { get; set; }

    public override Task LoadAsync() => RunBusyAsync(SearchCoreAsync);

    [RelayCommand]
    private Task SearchAsync() => RunBusyAsync(SearchCoreAsync);

    [RelayCommand]
    private Task ExportToExcelAsync() => ExportAsync(Entries.ToList(),
    [
        new("Audit_Time", e => e.EventTime, "dd/MM/yyyy HH:mm:ss"),
        new("Audit_Username", e => e.Username),
        new("Audit_Action", e => e.ActionName),
        new("Audit_Result", e => Loc[$"AuditResult_{e.Result}"]),
        new("Audit_Host", e => e.UserHost),
        new("Audit_Object", e => e.ObjectName),
    ], "Audit_ExportTitle");

    private async Task SearchCoreAsync()
    {
        var rows = await _audit.GetAsync(UsernameFilter, FromDate, ToDate);
        Entries.Clear();
        foreach (var row in rows)
        {
            Entries.Add(row);
        }
    }
}
