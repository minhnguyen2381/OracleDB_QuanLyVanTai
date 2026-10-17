using Transport.Application.Abstractions;
using Transport.Domain.Errors;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeAuthService : IAuthService
{
    public BusinessRuleException? LoginError { get; set; }
    public (string Username, string Password)? LastLogin { get; private set; }
    public int LogoutCount { get; private set; }

    public Task LoginAsync(string username, string password, CancellationToken ct = default)
    {
        LastLogin = (username, password);
        return LoginError is null ? Task.CompletedTask : Task.FromException(LoginError);
    }

    public void Logout() => LogoutCount++;
}
