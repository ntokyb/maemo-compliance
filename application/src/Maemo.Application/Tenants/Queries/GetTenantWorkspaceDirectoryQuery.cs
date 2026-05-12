using MediatR;

namespace Maemo.Application.Tenants.Queries;

public sealed class TenantDirectoryRowDto
{
    public string Email { get; init; } = null!;
    public string? Name { get; init; }
    public string Role { get; init; } = null!;
    public string Status { get; init; } = null!;
    public DateTime? LastLoginAt { get; init; }
}

public sealed record GetTenantWorkspaceDirectoryQuery : IRequest<IReadOnlyList<TenantDirectoryRowDto>>;
