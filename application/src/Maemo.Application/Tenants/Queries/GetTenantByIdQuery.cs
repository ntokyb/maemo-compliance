using Maemo.Application.Tenants.Dtos;
using MediatR;

namespace Maemo.Application.Tenants.Queries;

public class GetTenantByIdQuery : IRequest<TenantDto>
{
    public Guid Id { get; set; }
}

