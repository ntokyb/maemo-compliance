namespace Maemo.Application.Common;

public interface ICurrentUserService
{
    string? UserId { get; }

    /// <summary>Primary email from OIDC claims, when present.</summary>
    string? UserEmail { get; }
}

