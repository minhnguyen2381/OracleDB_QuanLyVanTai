using Transport.Application.Abstractions;
using Transport.Domain.Entities;
using Transport.Domain.Enums;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeScheduleRepository : IScheduleRepository
{
    public List<string> Calls { get; } = [];
    public Schedule? LastSchedule { get; private set; }

    public Task AddAsync(Schedule schedule, CancellationToken ct = default) { LastSchedule = schedule; Calls.Add("add"); return Task.CompletedTask; }
    public Task UpdateAsync(Schedule schedule, CancellationToken ct = default) { LastSchedule = schedule; Calls.Add("update"); return Task.CompletedTask; }
    public Task DeleteAsync(int scheduleId, CancellationToken ct = default) { Calls.Add($"delete:{scheduleId}"); return Task.CompletedTask; }
    public Task SetStatusAsync(int scheduleId, ScheduleStatus status, CancellationToken ct = default) { Calls.Add($"status:{scheduleId}:{status}"); return Task.CompletedTask; }
    public Task CancelAsync(int scheduleId, string reason, CancellationToken ct = default) { Calls.Add($"cancel:{scheduleId}:{reason}"); return Task.CompletedTask; }
}
