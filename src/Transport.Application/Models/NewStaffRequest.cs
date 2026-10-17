using Transport.Domain.Enums;

namespace Transport.Application.Models;

public sealed record NewStaffRequest(string Username, string Password, string FullName, string? Phone, StaffRole Role);
