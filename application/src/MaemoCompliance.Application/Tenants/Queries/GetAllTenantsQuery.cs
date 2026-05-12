using MaemoCompliance.Application.Tenants.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Tenants.Queries;

public class GetAllTenantsQuery : IRequest<IReadOnlyList<TenantDto>>
{
}

