using Transport.Domain.Errors;
using Transport.Infrastructure.Persistence;

namespace Transport.UnitTests.Infrastructure;

public class OracleErrorMapperTests
{
    [Theory]
    [InlineData(20001)] [InlineData(20002)] [InlineData(20003)] [InlineData(20010)] [InlineData(20011)]
    [InlineData(20020)] [InlineData(20021)] [InlineData(20030)] [InlineData(20031)] [InlineData(20032)]
    [InlineData(20040)] [InlineData(20041)] [InlineData(20042)] [InlineData(20043)] [InlineData(20044)]
    [InlineData(20050)] [InlineData(20051)] [InlineData(20052)]
    [InlineData(20060)] [InlineData(20061)] [InlineData(20062)]
    public void Map_ApplicationErrors_KeepTheirNumber(int number)
    {
        var ex = OracleErrorMapper.Map(number, $"ORA-{number}: Something.");

        Assert.NotNull(ex);
        Assert.Equal(number, (int)ex.Code);
        Assert.Equal("Something.", ex.Message);
    }

    [Theory]
    [InlineData(1, BusinessErrorCode.DuplicateValue)]
    [InlineData(1017, BusinessErrorCode.InvalidCredentials)]
    [InlineData(1031, BusinessErrorCode.InsufficientPrivileges)]
    [InlineData(1920, BusinessErrorCode.UsernameAlreadyExists)]
    [InlineData(2292, BusinessErrorCode.ChildRecordExists)]
    [InlineData(2290, BusinessErrorCode.CheckConstraintViolated)]
    [InlineData(28000, BusinessErrorCode.AccountLocked)]
    [InlineData(28001, BusinessErrorCode.PasswordExpired)]
    [InlineData(28003, BusinessErrorCode.InvalidPassword)]
    [InlineData(28007, BusinessErrorCode.PasswordReused)]
    [InlineData(12541, BusinessErrorCode.DatabaseUnavailable)]
    [InlineData(12514, BusinessErrorCode.DatabaseUnavailable)]
    public void Map_KnownSystemErrors(int number, BusinessErrorCode expected)
    {
        Assert.Equal(expected, OracleErrorMapper.Map(number, "ORA-00000: x")!.Code);
    }

    [Theory]
    [InlineData(942)]
    [InlineData(20999)]
    [InlineData(6550)]
    public void Map_UnknownErrors_ReturnNull(int number)
    {
        Assert.Null(OracleErrorMapper.Map(number, "ORA-00942: table or view does not exist"));
    }

    [Fact]
    public void CleanMessage_KeepsFirstLineWithoutOraPrefix()
    {
        const string raw = "ORA-20003: Seat 5 is already booked.\nORA-06512: at \"TRANSPORT.TRG_TICKETS_CHECK_SEAT\", line 40\nhttps://docs.oracle.com/error-help/db/ora-20003/";

        Assert.Equal("Seat 5 is already booked.", OracleErrorMapper.CleanMessage(raw));
    }
}
