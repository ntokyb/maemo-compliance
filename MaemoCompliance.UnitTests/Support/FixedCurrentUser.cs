using MaemoCompliance.Application.Common;

namespace MaemoCompliance.UnitTests.Support;

public sealed class FixedCurrentUser : ICurrentUserService
{
    public FixedCurrentUser(string userId, string? email = null)
    {
        UserId = userId;
        UserEmail = email;
    }

    public string? UserId { get; }
    public string? UserEmail { get; }
}
