using MaemoCompliance.Application.Common;

namespace MaemoCompliance.UnitTests.Support;

public sealed class FixedClock : IDateTimeProvider
{
    public FixedClock(DateTime utcNow) => UtcNow = utcNow;

    public DateTime UtcNow { get; set; }
}
