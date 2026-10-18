using Transport.Domain.Errors;
using Transport.Infrastructure.Persistence;

namespace Transport.UnitTests.Infrastructure;

public class OracleAuthServiceTests
{
    // Sai mật khẩu và bị khóa trả cùng một lỗi để không dò được tài khoản nào tồn tại/đang bị khóa
    [Theory]
    [InlineData(1017)]
    [InlineData(28000)]
    public void LoginErrors_DoNotRevealAccountState(int number)
    {
        var ex = OracleAuthService.MapLoginError(number, $"ORA-{number:D5}: original Oracle text");

        Assert.Equal(BusinessErrorCode.InvalidCredentials, ex!.Code);
        // Không mang theo thông điệp gốc của Oracle (VD "the account is locked")
        Assert.Equal("Invalid username or password, or the account is locked.", ex.Message);
    }

    [Theory]
    [InlineData(28001, BusinessErrorCode.PasswordExpired)]
    [InlineData(28003, BusinessErrorCode.InvalidPassword)]
    [InlineData(28007, BusinessErrorCode.PasswordReused)]
    [InlineData(12541, BusinessErrorCode.DatabaseUnavailable)]
    public void OtherLoginErrors_UseTheCommonMapping(int number, BusinessErrorCode expected)
    {
        Assert.Equal(expected, OracleAuthService.MapLoginError(number, "ORA-00000: x")!.Code);
    }

    [Fact]
    public void UnknownErrors_AreNotMapped()
    {
        Assert.Null(OracleAuthService.MapLoginError(942, "ORA-00942: table or view does not exist"));
    }
}
