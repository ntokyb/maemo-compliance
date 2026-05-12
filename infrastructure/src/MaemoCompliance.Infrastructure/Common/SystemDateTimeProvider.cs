using MaemoCompliance.Application.Common;

namespace MaemoCompliance.Infrastructure.Common;

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}

