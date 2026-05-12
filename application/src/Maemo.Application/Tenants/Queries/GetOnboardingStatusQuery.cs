using Maemo.Application.Tenants.Dtos;
using MediatR;

namespace Maemo.Application.Tenants.Queries;

public sealed record GetOnboardingStatusQuery : IRequest<OnboardingStatusDto>;
