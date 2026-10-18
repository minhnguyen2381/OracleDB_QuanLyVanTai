using Transport.Application.Abstractions;
using Transport.Domain.Errors;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeAuthService : IAuthService
{
    public BusinessRuleException? LoginError { get; set; }
    public BusinessRuleException? NewPasswordError { get; set; }
    public BusinessRuleException? ChangeError { get; set; }
    public (string Username, string Password)? LastLogin { get; private set; }
    public (string Username, string OldPassword, string NewPassword)? LastNewPasswordLogin { get; private set; }
    public (string CurrentPassword, string NewPassword)? LastChange { get; private set; }
    public int LogoutCount { get; private set; }

    public Task LoginAsync(string username, string password, CancellationToken ct = default)
    {
        LastLogin = (username, password);
        return LoginError is null ? Task.CompletedTask : Task.FromException(LoginError);
    }

    public Task LoginWithNewPasswordAsync(string username, string oldPassword, string newPassword, CancellationToken ct = default)
    {
        LastNewPasswordLogin = (username, oldPassword, newPassword);
        return NewPasswordError is null ? Task.CompletedTask : Task.FromException(NewPasswordError);
    }

    public Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct = default)
    {
        LastChange = (currentPassword, newPassword);
        return ChangeError is null ? Task.CompletedTask : Task.FromException(ChangeError);
    }

    public void Logout() => LogoutCount++;
}
