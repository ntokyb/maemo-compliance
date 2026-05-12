using Maemo.Application.Common;
using MediatR;

namespace Maemo.Application.Tenants.Queries;

public sealed record TestTenantSharePointConnectionQuery(Guid TenantId) : IRequest<SharePointConnectionTestResult>;
