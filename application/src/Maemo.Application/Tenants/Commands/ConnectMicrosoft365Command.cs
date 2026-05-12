using Maemo.Application.Tenants.Dtos;
using MediatR;

namespace Maemo.Application.Tenants.Commands;

public class ConnectMicrosoft365Command : IRequest
{
    public Guid TenantId { get; set; }
    public ConnectMicrosoft365Request Request { get; set; } = null!;
}

