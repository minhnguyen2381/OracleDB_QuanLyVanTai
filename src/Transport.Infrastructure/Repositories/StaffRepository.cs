using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Entities;
using Transport.Domain.Enums;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Repositories;

// Mọi thay đổi đi qua PKG_STAFF (tạo/khóa user Oracle)
internal sealed class StaffRepository(OracleConnectionFactory factory) : OracleRepository(factory), IStaffRepository
{
    // 'ROLE_TICKET_SELLER' -> 'TICKETSELLER' để Dapper parse thành StaffRole.TicketSeller
    public Task<IReadOnlyList<StaffMember>> GetAllAsync(CancellationToken ct = default) => QueryAsync<StaffMember>(
        """
        SELECT username, full_name, phone, REPLACE(SUBSTR(role_name, 6), '_', '') AS role,
               status, created_at, created_by
          FROM staff
         ORDER BY username
        """, null, ct);

    public Task CreateAsync(NewStaffRequest request, CancellationToken ct = default) => CallAsync("pkg_staff.create_staff", new
    {
        p_username = request.Username,
        p_password = request.Password,
        p_full_name = request.FullName,
        p_phone = request.Phone,
        p_role = DbEnum.RoleToDb(request.Role),
    }, ct);

    public Task UpdateInfoAsync(string username, string fullName, string? phone, CancellationToken ct = default) =>
        CallAsync("pkg_staff.update_staff_info", new { p_username = username, p_full_name = fullName, p_phone = phone }, ct);

    public Task ChangeRoleAsync(string username, StaffRole role, CancellationToken ct = default) =>
        CallAsync("pkg_staff.change_role", new { p_username = username, p_role = DbEnum.RoleToDb(role) }, ct);

    public Task ResetPasswordAsync(string username, string password, CancellationToken ct = default) =>
        CallAsync("pkg_staff.reset_password", new { p_username = username, p_password = password }, ct);

    public Task SetLockedAsync(string username, bool locked, CancellationToken ct = default) =>
        CallAsync("pkg_staff.set_locked", new { p_username = username, p_locked = locked ? 1 : 0 }, ct);
}
