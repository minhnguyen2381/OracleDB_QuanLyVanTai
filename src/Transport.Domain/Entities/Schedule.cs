using Transport.Domain.Enums;

namespace Transport.Domain.Entities;

// Lịch chạy (dùng khi thêm/sửa; danh sách đọc từ V_TRIP_STATISTICS)
public sealed record Schedule
{
    public int Id { get; init; }
    public int RouteId { get; init; }
    public int VehicleId { get; init; }
    public int DriverId { get; init; }
    public DateTime DepartureTime { get; init; }
    public DateTime ArrivalTime { get; init; }
    public decimal TicketPrice { get; init; }
    public ScheduleStatus Status { get; init; }
}
