using Transport.Domain.Entities;
using Transport.Domain.Enums;

namespace Transport.Application.Abstractions;

// Ghi lịch chạy; đọc danh sách qua IStatisticsQueries (view V_TRIP_STATISTICS)
public interface IScheduleRepository
{
    Task AddAsync(Schedule schedule, CancellationToken ct = default);
    Task UpdateAsync(Schedule schedule, CancellationToken ct = default);
    Task DeleteAsync(int scheduleId, CancellationToken ct = default);
    Task SetStatusAsync(int scheduleId, ScheduleStatus status, CancellationToken ct = default);
    // PKG_SCHEDULE.CANCEL_SCHEDULE: hủy vé, hoàn tiền rồi hủy chuyến
    Task CancelAsync(int scheduleId, string reason, CancellationToken ct = default);
}
