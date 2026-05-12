using Maemo.Application.Common;

namespace Maemo.Infrastructure.Common;

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}

