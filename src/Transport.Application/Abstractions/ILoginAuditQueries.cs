using Transport.Application.Models;

namespace Transport.Application.Abstractions;

// Đọc V_LOGIN_AUDIT (quyền SELECT chỉ cấp cho ROLE_OPERATOR)
public interface ILoginAuditQueries
{
    // from/to tính theo ngày, gồm cả ngày "to"; username null/rỗng = mọi tài khoản
    Task<IReadOnlyList<LoginAuditEntry>> GetAsync(string? username, DateTime from, DateTime to, CancellationToken ct = default);
}
