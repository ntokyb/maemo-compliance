using MediatR;

namespace MaemoCompliance.Application.AccessRequests.Commands;

public sealed record RejectAccessRequestCommand(Guid Id, string? Reason) : IRequest;
