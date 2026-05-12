using Maemo.Application.Tenants.Dtos;
using MediatR;

namespace Maemo.Application.Tenants.Queries;

public class GetAllTenantsQuery : IRequest<IReadOnlyList<TenantDto>>
{
}

