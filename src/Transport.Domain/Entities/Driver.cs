using Transport.Domain.Enums;

namespace Transport.Domain.Entities;

// Tài xế
public sealed record Driver
{
    // Cảnh báo khi bằng lái còn dưới số ngày này
    public const int ExpiryWarningDays = 30;

    public int Id { get; init; }
    public required string FullName { get; init; }
    public required string IdCard { get; init; }
    public required string Phone { get; init; }
    public required string LicenseNo { get; init; }
    public LicenseClass LicenseClass { get; init; }
    public DateTime LicenseExpiry { get; init; }
    public DriverStatus Status { get; init; }

    // Tình trạng bằng lái so với ngày tham chiếu
    public LicenseState GetLicenseState(DateTime today) =>
        LicenseExpiry.Date < today.Date ? LicenseState.Expired
        : LicenseExpiry.Date < today.Date.AddDays(ExpiryWarningDays) ? LicenseState.ExpiringSoon
        : LicenseState.Valid;

    // Có được phân công chạy chuyến khởi hành vào thời điểm này không
    public bool CanDrive(DateTime departure) =>
        Status == DriverStatus.Active && LicenseExpiry.Date >= departure.Date;
}
