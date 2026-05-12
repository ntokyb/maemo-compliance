using MaemoCompliance.Application.Tenants.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Tenants.Commands;

public class ConnectMicrosoft365Command : IRequest
{
    public Guid TenantId { get; set; }
    public ConnectMicrosoft365Request Request { get; set; } = null!;
}

