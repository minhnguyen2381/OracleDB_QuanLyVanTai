using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Entities;
using Transport.Domain.Enums;
using Transport.Domain.Errors;

namespace Transport.Application.Services;

// Quản lý nhân viên (user Oracle) – chỉ điều hành
public sealed class StaffService(IStaffRepository staff, IUserSession session)
{
    public async Task<IReadOnlyList<StaffMember>> GetAllAsync(CancellationToken ct = default)
    {
        Rules.Operator(session);
        return await staff.GetAllAsync(ct);
    }

    public async Task CreateAsync(NewStaffRequest request, CancellationToken ct = default)
    {
        Rules.Operator(session);
        Rules.Username(request.Username);
        Rules.Password(request.Password, request.Username);
        Rules.Required(request.FullName);
        Rules.OptionalPhone(request.Phone);
        await staff.CreateAsync(request with { FullName = request.FullName.Trim(), Phone = Rules.TrimToNull(request.Phone) }, ct);
    }

    public async Task UpdateInfoAsync(string username, string fullName, string? phone, CancellationToken ct = default)
    {
        Rules.Operator(session);
        Rules.Required(fullName);
        Rules.OptionalPhone(phone);
        await staff.UpdateInfoAsync(username, fullName.Trim(), Rules.TrimToNull(phone), ct);
    }

    public async Task ChangeRoleAsync(string username, StaffRole role, CancellationToken ct = default)
    {
        Rules.Operator(session);
        NotSelf(username);
        await staff.ChangeRoleAsync(username, role, ct);
    }

    public async Task ResetPasswordAsync(string username, string password, CancellationToken ct = default)
    {
        Rules.Operator(session);
        Rules.Password(password, username);
        await staff.ResetPasswordAsync(username, password, ct);
    }

    public async Task SetLockedAsync(string username, bool locked, CancellationToken ct = default)
    {
        Rules.Operator(session);
        NotSelf(username);
        await staff.SetLockedAsync(username, locked, ct);
    }

    // Không cho tự khóa/tự đổi quyền chính mình (giống PKG_STAFF.not_self)
    private void NotSelf(string username)
    {
        if (string.Equals(username, session.Username, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException(BusinessErrorCode.CannotChangeOwnAccount, "You cannot lock or change your own account.");
        }
    }
}
