using Transport.Application.Services;
using Transport.Domain.Errors;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Application;

public class RegistrationServiceTests
{
    private readonly FakeRegistrationRepository _repository = new();

    private RegistrationService Create() => new(_repository);

    [Fact]
    public async Task Submit_WorksWithoutSignInAndTrimsInput()
    {
        await Create().SubmitAsync(" newuser01 ", " Người Mới ", " ");

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
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => Create().SubmitAsync(username, fullName, phone));

        Assert.Equal(expected, ex.Code);
        Assert.Empty(_repository.Calls);
    }
}
