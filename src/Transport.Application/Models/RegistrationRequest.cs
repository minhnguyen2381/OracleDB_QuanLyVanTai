using Transport.Domain.Enums;

namespace Transport.Application.Models;

// Một dòng REGISTRATION_REQUESTS (không có mật khẩu)
public sealed record RegistrationRequest
{
    public int RequestId { get; init; }
    public required string Username { get; init; }
    public required string FullName { get; init; }
    public string? Phone { get; init; }
    public RegistrationStatus Status { get; init; }
    public string? ClientHost { get; init; }
    public DateTime RequestedAt { get; init; }
    public string? ReviewedBy { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? RejectReason { get; init; }
}
