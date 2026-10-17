using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Application.Models;
using Transport.Application.Services;

namespace Transport.Presentation.Wpf.ViewModels.Pages;

// Sao lưu lịch chạy + vé + thanh toán (BACKUP_OPERATIONAL_DATA) và theo dõi job sao lưu đêm
public sealed partial class BackupViewModel : PageViewModel
{
    private readonly BackupService _backups;

    public BackupViewModel(PageServices services, BackupService backups)
        : base(services)
    {
        _backups = backups;
        JobStateText = string.Empty;
    }

    public ObservableCollection<BackupLog> Logs { get; } = [];

    [ObservableProperty]
    public partial BackupJobStatus? JobStatus { get; set; }

    [ObservableProperty]
    public partial string JobStateText { get; set; }

    public override Task LoadAsync() => RunBusyAsync(LoadCoreAsync);

    [RelayCommand]
    private Task BackupNowAsync() => RunBusyAsync(async () =>
    {
        await _backups.RunAsync();
        await LoadCoreAsync();
        if (Logs.FirstOrDefault() is { } latest)
        {
            Notify.ShowSuccess(Loc.Format("Backup_Done", latest.LogId, latest.ScheduleRows ?? 0,
                latest.TicketRows ?? 0, latest.PaymentRows ?? 0));
        }
    });

    [RelayCommand]
    private Task ExportToExcelAsync() => ExportAsync(Logs.ToList(),
    [
        new("Backup_LogId", b => b.LogId),
        new("Backup_At", b => b.BackupAt, "dd/MM/yyyy HH:mm"),
        new("Backup_By", b => b.PerformedBy),
        new("Backup_Schedules", b => b.ScheduleRows),
        new("Backup_Tickets", b => b.TicketRows),
        new("Backup_Payments", b => b.PaymentRows),
    ], "Backup_ExportTitle");

    private async Task LoadCoreAsync()
    {
        var logs = await _backups.GetLogsAsync();
        Logs.Clear();
        foreach (var log in logs)
        {
            Logs.Add(log);
        }

        JobStatus = await _backups.GetJobStatusAsync();
        JobStateText = Loc[JobStatus?.IsEnabled == true ? "Backup_JobEnabled" : "Backup_JobDisabled"];
    }
}
