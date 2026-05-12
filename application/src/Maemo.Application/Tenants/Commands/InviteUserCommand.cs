using MediatR;

namespace Maemo.Application.Tenants.Commands;

public sealed record InviteUserCommand(string Email, string RoleName, string PortalBaseUrl) : IRequest<Guid>;
