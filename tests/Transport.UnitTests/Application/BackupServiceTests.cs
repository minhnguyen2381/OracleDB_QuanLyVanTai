using Transport.Application.Services;
using Transport.Domain.Errors;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Application;

public class BackupServiceTests
{
    [Fact]
    public async Task Operator_CanRunBackup()
    {
        var repository = new FakeBackupRepository();

        await new BackupService(repository, new FakeUserSession()).RunAsync();

        Assert.Equal(1, repository.RunCount);
    }

    [Fact]
    public async Task Seller_CannotRunBackup()
    {
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => new BackupService(new FakeBackupRepository(), FakeUserSession.Seller()).RunAsync());

        Assert.Equal(BusinessErrorCode.OperatorOnly, ex.Code);
    }
}
