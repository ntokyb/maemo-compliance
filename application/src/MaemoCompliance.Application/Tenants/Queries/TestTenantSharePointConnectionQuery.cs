using MaemoCompliance.Application.Common;
using MediatR;

namespace MaemoCompliance.Application.Tenants.Queries;

public sealed record TestTenantSharePointConnectionQuery(Guid TenantId) : IRequest<SharePointConnectionTestResult>;
