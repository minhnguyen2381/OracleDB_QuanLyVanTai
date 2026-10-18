using Transport.Application.Models;
using Transport.Presentation.Wpf.ViewModels.Pages;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class LoginAuditViewModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 10, 0, 0, TimeSpan.Zero);
    private readonly PageTestContext _context = new();
    private readonly FakeLoginAuditQueries _audit = new();

    public LoginAuditViewModelTests() => _audit.Entries.Add(new LoginAuditEntry
    {
        EventTime = new DateTime(2026, 10, 18, 9, 0, 0),
        Username = "SELLER01",
        ActionName = "LOGON",
        Result = "FAILED",
        ReturnCode = 1017,
    });

    private LoginAuditViewModel Create() => new(_context.Services, _audit, new FixedTimeProvider(Now));

    [Fact]
    public async Task Load_ShowsTheLastSevenDays()
    {
        var vm = Create();

        await vm.LoadAsync();

        Assert.Equal((string.Empty, new DateTime(2026, 10, 11), new DateTime(2026, 10, 18)), _audit.LastQuery);
        Assert.Single(vm.Entries);
    }

    [Fact]
    public async Task Search_UsesTheFilters()
    {
        var vm = Create();
        vm.UsernameFilter = "seller01";
        vm.FromDate = new DateTime(2026, 10, 1);

        await vm.SearchCommand.ExecuteAsync(null);

        Assert.Equal(("seller01", new DateTime(2026, 10, 1), new DateTime(2026, 10, 18)), _audit.LastQuery);
    }
}
