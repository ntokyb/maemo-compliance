using MediatR;

namespace MaemoCompliance.Application.AccessRequests.Commands;

public sealed record ApproveAccessRequestCommand(Guid Id, string CompanyName, string Plan) : IRequest;
