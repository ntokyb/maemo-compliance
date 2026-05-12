using MediatR;

namespace MaemoCompliance.Application.Public.Commands;

public sealed record SubmitPublicContactCommand(
    string Name,
    string Company,
    string Email,
    string Message) : IRequest;
