using Transport.Application.Models;
using Transport.Application.Services;
using Transport.Presentation.Wpf.ViewModels.Pages;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class BackupViewModelTests
{
    private readonly PageTestContext _context = new();
    private readonly FakeBackupRepository _repository = new();

    public BackupViewModelTests()
    {
        _repository.Logs.Add(new BackupLog
        {
            LogId = 3,
            BackupAt = new DateTime(2026, 10, 17, 2, 0, 0),
            PerformedBy = "TRANSPORT",
            ScheduleRows = 35,
            TicketRows = 210,
            PaymentRows = 190,
        });
        _repository.JobStatus = new BackupJobStatus
        {
            JobName = "JOB_NIGHTLY_BACKUP",
            Enabled = "TRUE",
            State = "SCHEDULED",
            NextRunDate = new DateTime(2026, 10, 18, 2, 0, 0),
            FailureCount = 0,
        };
    }

    private BackupViewModel Create() => new(_context.Services, new BackupService(_repository, _context.Session));

    [Fact]
    public async Task Load_ShowsLogsAndJobStatus()
    {
        var vm = Create();

        await vm.LoadAsync();

        Assert.Single(vm.Logs);
        Assert.Equal("Backup_JobEnabled", vm.JobStateText);
        Assert.Equal(new DateTime(2026, 10, 18, 2, 0, 0), vm.JobStatus!.NextRunDate);
    }

    [Fact]
    public async Task BackupNow_RunsAndReportsLatestCounts()
    {
        var vm = Create();

        await vm.BackupNowCommand.ExecuteAsync(null);

        Assert.Equal(1, _repository.RunCount);
        Assert.Equal(new[] { "Backup_Done|3,35,210,190" }, _context.Notifications.Successes);
    }
}
