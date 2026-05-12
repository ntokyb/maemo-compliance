using MediatR;

namespace MaemoCompliance.Application.Tenants.Commands;

public sealed record AcceptUserInvitationCommand(string Token) : IRequest;
