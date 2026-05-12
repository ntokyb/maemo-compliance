using MaemoCompliance.Application.Tenants.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Tenants.Queries;

public sealed record GetOnboardingStatusQuery : IRequest<OnboardingStatusDto>;
