using Transport.Application.Models;
using Transport.Domain.Entities;
using Transport.Domain.Enums;

namespace Transport.UnitTests.Fakes;

// Dữ liệu mẫu cho test
internal static class TestData
{
    public static Route Route(int id, string name = "Hà Nội - Lào Cai", decimal price = 280000, int? durationMin = 330,
        string origin = "Hà Nội", string destination = "Lào Cai") => new()
    {
        Id = id,
        Name = name,
        Origin = origin,
        Destination = destination,
        DistanceKm = 300,
        DurationMin = durationMin,
        BasePrice = price,
    };

    public static Vehicle Vehicle(int id, string plate = "29B-196.88", int seats = 40,
        VehicleStatus status = VehicleStatus.Active) => new()
    {
        Id = id,
        PlateNumber = plate,
        VehicleType = "Giường nằm 40 chỗ",
        SeatCount = seats,
        Status = status,
    };

    public static Driver Driver(int id, string name = "Nguyễn Văn An", DateTime? expiry = null,
        DriverStatus status = DriverStatus.Active) => new()
    {
        Id = id,
        FullName = name,
        IdCard = $"0010850{id:00000}",
        Phone = $"09123456{id:00}",
        LicenseNo = $"7901234567{id:00}",
        LicenseClass = LicenseClass.D,
        LicenseExpiry = expiry ?? new DateTime(2030, 1, 1),
        Status = status,
    };

    public static Passenger Passenger(int id, string name = "Nguyễn Thị Hoa", string phone = "0987000001") => new()
    {
        Id = id,
        FullName = name,
        Phone = phone,
        IdCard = null,
        Email = null,
    };

    public static StaffMember Staff(string username, StaffRole role = StaffRole.TicketSeller,
        StaffStatus status = StaffStatus.Active) => new()
    {
        Username = username,
        FullName = "Trần Thị Bán",
        Phone = "0912000002",
        Role = role,
        Status = status,
        CreatedAt = new DateTime(2026, 10, 1),
        CreatedBy = "TRANSPORT",
    };

    public static TicketListItem Ticket(long id, TicketStatus status = TicketStatus.Booked, decimal price = 280000,
        string phone = "0987000001") => new()
    {
        TicketId = id,
        ScheduleId = 1,
        RouteName = "Hà Nội - Lào Cai",
        DepartureTime = new DateTime(2026, 10, 18, 7, 0, 0),
        PlateNumber = "29B-196.88",
        SeatNo = 5,
        PassengerName = "Nguyễn Thị Hoa",
        Phone = phone,
        Price = price,
        Status = status,
        BookedAt = new DateTime(2026, 10, 16, 9, 0, 0),
        SoldBy = "SELLER01",
    };

    public static TicketPrintModel PrintModel(long id) => new()
    {
        TicketId = id,
        RouteName = "Hà Nội - Lào Cai",
        Origin = "Hà Nội",
        Destination = "Lào Cai",
        DepartureTime = new DateTime(2026, 10, 18, 7, 0, 0),
        ArrivalTime = new DateTime(2026, 10, 18, 12, 30, 0),
        PlateNumber = "29B-196.88",
        VehicleType = "Giường nằm 40 chỗ",
        SeatNo = 5,
        PassengerName = "Nguyễn Thị Hoa",
        Phone = "0987000001",
        Price = 280000,
        Status = TicketStatus.Paid,
        SoldBy = "SELLER01",
        BookedAt = new DateTime(2026, 10, 16, 9, 0, 0),
    };
}
