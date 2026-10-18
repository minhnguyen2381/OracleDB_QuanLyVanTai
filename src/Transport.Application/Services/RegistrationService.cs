using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Enums;

namespace Transport.Application.Services;

// Đăng ký tài khoản: người chưa có tài khoản gửi yêu cầu, điều hành duyệt (cấp mật khẩu tạm) hoặc từ chối
public sealed class RegistrationService(IRegistrationRepository requests, IUserSession session)
{
    // Không cần đăng nhập; repository gọi bằng user TRANSPORT_REGISTRAR
    public Task SubmitAsync(string? username, string? fullName, string? phone, CancellationToken ct = default)
    {
        var user = username?.Trim();
        Rules.Username(user);
        Rules.Required(fullName);
        Rules.OptionalPhone(phone);
        return requests.SubmitAsync(user!, fullName!.Trim(), Rules.TrimToNull(phone), ct);
    }

    public async Task<IReadOnlyList<RegistrationRequest>> GetAllAsync(CancellationToken ct = default)
    {
        Rules.Operator(session);
        return await requests.GetAllAsync(ct);
    }

    // Mật khẩu tạm phải đạt luật PROFILE_STAFF; nhân viên mới phải đổi ở lần đăng nhập đầu
    public async Task ApproveAsync(RegistrationRequest request, string tempPassword, StaffRole role, CancellationToken ct = default)
    {
        Rules.Operator(session);
        Rules.Password(tempPassword, request.Username);
        await requests.ApproveAsync(request.RequestId, tempPassword, role, ct);
    }

    public async Task RejectAsync(int requestId, string reason, CancellationToken ct = default)
    {
        Rules.Operator(session);
        Rules.Required(reason);
        await requests.RejectAsync(requestId, reason.Trim(), ct);
    }
}
