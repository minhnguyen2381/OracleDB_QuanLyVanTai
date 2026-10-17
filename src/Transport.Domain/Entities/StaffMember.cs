using Transport.Domain.Enums;

namespace Transport.Domain.Entities;

// Nhân viên = một user Oracle (bảng STAFF)
public sealed record StaffMember
{
    public required string Username { get; init; }
    public required string FullName { get; init; }
    public string? Phone { get; init; }
    public StaffRole Role { get; init; }
    public StaffStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public required string CreatedBy { get; init; }
}
