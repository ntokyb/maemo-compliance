using MaemoCompliance.Application.Tenants.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Tenants.Queries;

public class GetTenantByIdQuery : IRequest<TenantDto>
{
    public Guid Id { get; set; }
}

