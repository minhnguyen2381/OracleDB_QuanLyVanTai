namespace Transport.UnitTests.Fakes;

// Đồng hồ cố định cho test (múi giờ UTC để GetLocalNow() = now)
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now.ToUniversalTime();

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
