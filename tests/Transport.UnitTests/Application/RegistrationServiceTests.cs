using Transport.Application.Models;
using Transport.Application.Services;
using Transport.Domain.Enums;
using Transport.Domain.Errors;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Application;

public class RegistrationServiceTests
{
    private readonly FakeRegistrationRepository _repository = new();

    private static readonly FakeUserSession Anonymous = new() { Username = string.Empty, FullName = string.Empty, IsOperator = false, IsAuthenticated = false };

    private static readonly RegistrationRequest Pending = new()
    {
        RequestId = 7,
        Username = "NEWUSER01",
        FullName = "Người Mới",
        Status = RegistrationStatus.Pending,
        RequestedAt = new DateTime(2026, 10, 18, 9, 0, 0),
    };

    private RegistrationService Create(FakeUserSession session) => new(_repository, session);

    [Fact]
    public async Task Submit_WorksWithoutSignInAndTrimsInput()
    {
        await Create(Anonymous).SubmitAsync(" newuser01 ", " Người Mới ", " ");

        Assert.Equal(new[] { "submit:newuser01:Người Mới:" }, _repository.Calls);
    }

    [Theory]
    [InlineData(null, "Tên", null, BusinessErrorCode.InvalidUsername)]
    [InlineData("ab", "Tên", null, BusinessErrorCode.InvalidUsername)]
    [InlineData("x; drop", "Tên", null, BusinessErrorCode.InvalidUsername)]
    [InlineData("newuser01", " ", null, BusinessErrorCode.RequiredField)]
    [InlineData("newuser01", "Tên", "123", BusinessErrorCode.InvalidPhone)]
    public async Task Submit_ValidatesBeforeCallingOracle(string? username, string fullName, string? phone, BusinessErrorCode expected)
    {
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => Create(Anonymous).SubmitAsync(username, fullName, phone));

        Assert.Equal(expected, ex.Code);
        Assert.Empty(_repository.Calls);
    }

    [Fact]
    public async Task Approve_RequiresAStrongTemporaryPassword()
    {
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => Create(new FakeUserSession()).ApproveAsync(Pending, "weakpass", StaffRole.TicketSeller));

        Assert.Equal(BusinessErrorCode.InvalidPassword, ex.Code);
        Assert.Empty(_repository.Calls);
    }

    [Fact]
    public async Task Approve_And_Reject_ReachRepository()
    {
        var service = Create(new FakeUserSession());

        await service.ApproveAsync(Pending, "TempPass#1", StaffRole.TicketSeller);
        await service.RejectAsync(7, " Trùng người ");

        Assert.Equal(new[] { "approve:7:TicketSeller", "reject:7:Trùng người" }, _repository.Calls);
    }

    [Fact]
    public async Task Reject_RequiresReason()
    {
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => Create(new FakeUserSession()).RejectAsync(7, " "));

        Assert.Equal(BusinessErrorCode.RequiredField, ex.Code);
    }

    [Fact]
    public async Task Sellers_CannotReviewRequests()
    {
        var service = Create(FakeUserSession.Seller());

        Assert.Equal(BusinessErrorCode.OperatorOnly, (await Assert.ThrowsAsync<BusinessRuleException>(() => service.GetAllAsync())).Code);
        Assert.Equal(BusinessErrorCode.OperatorOnly, (await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.ApproveAsync(Pending, "TempPass#1", StaffRole.TicketSeller))).Code);
        Assert.Equal(BusinessErrorCode.OperatorOnly, (await Assert.ThrowsAsync<BusinessRuleException>(() => service.RejectAsync(7, "x"))).Code);
    }
}
