using Transport.Application.Models;
using Transport.Application.Services;
using Transport.Domain.Enums;
using Transport.Presentation.Wpf.ViewModels.Dialogs;
using Transport.Presentation.Wpf.ViewModels.Pages;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class RegistrationRequestsViewModelTests
{
    private readonly PageTestContext _context = new();
    private readonly FakeRegistrationRepository _repository = new();

    public RegistrationRequestsViewModelTests()
    {
        _repository.Requests.Add(new RegistrationRequest
        {
            RequestId = 7, Username = "NEWUSER01", FullName = "Người Mới",
            Status = RegistrationStatus.Pending, RequestedAt = new DateTime(2026, 10, 18, 9, 0, 0),
        });
        _repository.Requests.Add(new RegistrationRequest
        {
            RequestId = 6, Username = "OLDUSER01", FullName = "Người Cũ",
            Status = RegistrationStatus.Approved, RequestedAt = new DateTime(2026, 10, 17, 9, 0, 0),
        });
    }

    private RegistrationRequestsViewModel Create() => new(_context.Services, new RegistrationService(_repository, _context.Session));

    [Fact]
    public async Task Load_ShowsPendingOnlyByDefault()
    {
        var vm = Create();

        await vm.LoadAsync();
        Assert.Equal(new[] { "NEWUSER01" }, vm.Items.Select(r => r.Username));

        vm.ShowOnlyPending = false;
        Assert.Equal(2, vm.Items.Count);
    }

    [Fact]
    public async Task Approve_SendsDialogInputAndReloads()
    {
        _context.Dialogs.ApprovalResult = new ApprovalInput("TempPass#1", StaffRole.Operator);
        var vm = Create();
        await vm.LoadAsync();
        vm.SelectedItem = vm.Items[0];

        await vm.ApproveCommand.ExecuteAsync(null);

        Assert.Equal(new[] { "Reg_ApproveTitle|NEWUSER01" }, _context.Dialogs.Prompts);
        Assert.Equal(new[] { "approve:7:Operator" }, _repository.Calls);
        Assert.Equal(new[] { "Reg_Approved" }, _context.Notifications.Successes);
    }

    [Fact]
    public async Task Approve_Cancelled_DoesNothing()
    {
        var vm = Create();
        await vm.LoadAsync();
        vm.SelectedItem = vm.Items[0];

        await vm.ApproveCommand.ExecuteAsync(null);

        Assert.Empty(_repository.Calls);
    }

    [Fact]
    public async Task Reject_SendsReason()
    {
        _context.Dialogs.PromptTextResult = "Trùng người";
        var vm = Create();
        await vm.LoadAsync();
        vm.SelectedItem = vm.Items[0];

        await vm.RejectCommand.ExecuteAsync(null);

        Assert.Equal(new[] { "reject:7:Trùng người" }, _repository.Calls);
        Assert.Equal(new[] { "Reg_Rejected" }, _context.Notifications.Successes);
    }

    [Fact]
    public async Task ReviewedRequests_CannotBeApprovedOrRejected()
    {
        var vm = Create();
        await vm.LoadAsync();
        vm.ShowOnlyPending = false;

        vm.SelectedItem = vm.Items.Single(r => r.Status == RegistrationStatus.Approved);

        Assert.False(vm.ApproveCommand.CanExecute(null));
        Assert.False(vm.RejectCommand.CanExecute(null));
    }
}
