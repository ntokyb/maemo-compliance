using MediatR;

namespace Maemo.Application.Tenants.Queries;

public sealed record InvitePreviewDto(bool Valid, string? CompanyName, string? Email, string? Message);

public sealed record ValidateInviteTokenQuery(string Token) : IRequest<InvitePreviewDto>;
