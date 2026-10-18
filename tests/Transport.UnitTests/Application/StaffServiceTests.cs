using Transport.Application.Models;
using Transport.Application.Services;
using Transport.Domain.Enums;
using Transport.Domain.Errors;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Application;

public class StaffServiceTests
{
    private readonly FakeStaffRepository _repository = new();
    private readonly StaffService _service;

    public StaffServiceTests() => _service = new StaffService(_repository, new FakeUserSession { Username = "OPERATOR01" });

    [Fact]
    public async Task CreateAsync_NormalizesAndCreates()
    {
        await _service.CreateAsync(new NewStaffRequest("seller03", "Seller#003", " Phạm Thị C ", " ", StaffRole.TicketSeller));

        Assert.Equal(new NewStaffRequest("seller03", "Seller#003", "Phạm Thị C", null, StaffRole.TicketSeller), _repository.LastCreate);
    }

    [Theory]
    [InlineData("ab", "Password1", BusinessErrorCode.InvalidUsername)]
    [InlineData("1seller", "Password1", BusinessErrorCode.InvalidUsername)]
    [InlineData("x; drop", "Password1", BusinessErrorCode.InvalidUsername)]
    [InlineData("seller03", "short", BusinessErrorCode.InvalidPassword)]
    [InlineData("seller03", "has\"quote1", BusinessErrorCode.InvalidPassword)]
    [InlineData("seller03", "alllower#1", BusinessErrorCode.InvalidPassword)]
    [InlineData("seller03", "ALLUPPER#1", BusinessErrorCode.InvalidPassword)]
    [InlineData("seller03", "NoDigits#x", BusinessErrorCode.InvalidPassword)]
    [InlineData("seller03", "NoSpecial12", BusinessErrorCode.InvalidPassword)]
    [InlineData("seller03", "Seller03#x", BusinessErrorCode.InvalidPassword)]
    public async Task CreateAsync_EnforcesSameRulesAsPkgStaff(string username, string password, BusinessErrorCode expected)
    {
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.CreateAsync(new NewStaffRequest(username, password, "Tên", null, StaffRole.Operator)));

        Assert.Equal(expected, ex.Code);
        Assert.Empty(_repository.Calls);
    }

    [Fact]
    public async Task ResetPassword_RejectsWeakPassword()
    {
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.ResetPasswordAsync("SELLER01", "weakpass"));

        Assert.Equal(BusinessErrorCode.InvalidPassword, ex.Code);
        Assert.Empty(_repository.Calls);
    }

    [Fact]
    public async Task CannotLockOrChangeRoleOfOwnAccount()
    {
        var lockSelf = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.SetLockedAsync("operator01", true));
        var roleSelf = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.ChangeRoleAsync("OPERATOR01", StaffRole.TicketSeller));

        Assert.Equal(BusinessErrorCode.CannotChangeOwnAccount, lockSelf.Code);
        Assert.Equal(BusinessErrorCode.CannotChangeOwnAccount, roleSelf.Code);
    }

    [Fact]
    public async Task OtherOperationsReachRepository()
    {
        await _service.UpdateInfoAsync("SELLER01", " Trần Thị Bán ", "0912000002");
        await _service.ChangeRoleAsync("SELLER01", StaffRole.Operator);
        await _service.ResetPasswordAsync("SELLER01", "NewPass#01");
        await _service.SetLockedAsync("SELLER01", true);

        Assert.Equal(
            new[] { "update:SELLER01:Trần Thị Bán:0912000002", "role:SELLER01:Operator", "password:SELLER01", "lock:SELLER01:True" },
            _repository.Calls);
    }

    [Fact]
    public async Task Seller_CannotManageStaff()
    {
        var service = new StaffService(_repository, FakeUserSession.Seller());

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.GetAllAsync());

        Assert.Equal(BusinessErrorCode.OperatorOnly, ex.Code);
    }
}
