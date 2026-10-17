using Transport.Application.Models;
using Transport.Application.Services;
using Transport.Domain.Enums;
using Transport.Presentation.Wpf.ViewModels.Pages;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class StaffViewModelTests
{
    private readonly PageTestContext _context = new();
    private readonly FakeStaffRepository _repository = new();

    public StaffViewModelTests()
    {
        _repository.Staff.Add(TestData.Staff("OPERATOR01", StaffRole.Operator));
        _repository.Staff.Add(TestData.Staff("SELLER01"));
    }

    private StaffViewModel Create() => new(_context.Services, new StaffService(_repository, _context.Session));

    private async Task<StaffViewModel> CreateWithSellerSelectedAsync()
    {
        var vm = Create();
        await vm.LoadAsync();
        vm.SelectedItem = vm.Items.Single(s => s.Username == "SELLER01");
        return vm;
    }

    [Fact]
    public async Task New_CreatesOracleUserWithChosenRole()
    {
        var vm = Create();
        vm.NewCommand.Execute(null);
        vm.Username = "seller03";
        vm.Password = "Seller#003";
        vm.FullName = "Phạm Thị C";
        vm.Role = StaffRole.TicketSeller;

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal(new NewStaffRequest("seller03", "Seller#003", "Phạm Thị C", null, StaffRole.TicketSeller), _repository.LastCreate);
    }

    [Fact]
    public async Task New_WithShortPassword_ShowsTranslatedError()
    {
        var vm = Create();
        vm.NewCommand.Execute(null);
        vm.Username = "seller03";
        vm.Password = "short";
        vm.FullName = "Phạm Thị C";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Empty(_repository.Calls);
        Assert.Equal(new[] { "Err_20044" }, _context.Notifications.Errors);
    }

    [Fact]
    public async Task Edit_UpdatesInfo_AndChangesRoleOnlyWhenChanged()
    {
        var vm = await CreateWithSellerSelectedAsync();

        vm.EditCommand.Execute(null);
        vm.FullName = "Trần Thị Bán B";
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal(new[] { "update:SELLER01:Trần Thị Bán B:0912000002" }, _repository.Calls);

        vm.EditCommand.Execute(null);
        vm.Role = StaffRole.Operator;
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Contains("role:SELLER01:Operator", _repository.Calls);
    }

    [Fact]
    public async Task ResetPassword_UsesPasswordDialog()
    {
        var vm = await CreateWithSellerSelectedAsync();
        _context.Dialogs.PromptPasswordResult = "NewPass#01";

        await vm.ResetPasswordCommand.ExecuteAsync(null);

        Assert.Equal(new[] { "password:SELLER01" }, _repository.Calls);
        Assert.Equal("Staff_ResetTitle|SELLER01", _context.Dialogs.Prompts[0]);
        Assert.Equal(new[] { "Staff_PasswordReset" }, _context.Notifications.Successes);
    }

    [Fact]
    public async Task ToggleLock_LocksActiveAccountAfterConfirmation()
    {
        var vm = await CreateWithSellerSelectedAsync();

        await vm.ToggleLockCommand.ExecuteAsync(null);

        Assert.Equal("Staff_ConfirmLock|SELLER01", _context.Dialogs.ConfirmMessages[0]);
        Assert.Equal(new[] { "lock:SELLER01:True" }, _repository.Calls);
    }

    [Fact]
    public void StaffCannotBeDeleted()
    {
        Assert.False(Create().CanDeleteItems);
    }
}
