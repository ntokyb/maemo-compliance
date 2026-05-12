using MediatR;

namespace Maemo.Application.Tenants.Commands;

public sealed record AcceptUserInvitationCommand(string Token) : IRequest;
