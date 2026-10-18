using Transport.Application.Services;
using Transport.Domain.Errors;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Application;

public class RegistrationServiceTests
{
    private readonly FakeRegistrationRepository _repository = new();

    private RegistrationService Create() => new(_repository);

    [Fact]
    public async Task Register_WorksWithoutSignInAndTrimsInput()
    {
        await Create().RegisterAsync(" newuser01 ", "Secret#123", " Người Mới ", " ");

        Assert.Equal(new[] { "register:newuser01:Secret#123:Người Mới:" }, _repository.Calls);
    }

    [Theory]
    [InlineData(null, "Secret#123", "Tên", null, BusinessErrorCode.InvalidUsername)]
    [InlineData("ab", "Secret#123", "Tên", null, BusinessErrorCode.InvalidUsername)]
    [InlineData("x; drop", "Secret#123", "Tên", null, BusinessErrorCode.InvalidUsername)]
    [InlineData("newuser01", null, "Tên", null, BusinessErrorCode.InvalidPassword)]
    [InlineData("newuser01", "weakpass", "Tên", null, BusinessErrorCode.InvalidPassword)]
    [InlineData("newuser01", "Newuser01#", "Tên", null, BusinessErrorCode.InvalidPassword)]
    [InlineData("newuser01", "Secret#123", " ", null, BusinessErrorCode.RequiredField)]
    [InlineData("newuser01", "Secret#123", "Tên", "123", BusinessErrorCode.InvalidPhone)]
    public async Task Register_ValidatesBeforeCallingOracle(
        string? username, string? password, string fullName, string? phone, BusinessErrorCode expected)
    {
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => Create().RegisterAsync(username, password, fullName, phone));

        Assert.Equal(expected, ex.Code);
        Assert.Empty(_repository.Calls);
    }
}
