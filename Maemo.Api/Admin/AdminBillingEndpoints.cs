using Maemo.Application.Billing;
using Maemo.Application.Common;
using MediatR;

namespace Maemo.Api.Admin;

/// <summary>
/// Admin Billing API endpoints - Billing management for Codist staff.
/// These endpoints are for internal platform operations.
/// </summary>
public static class AdminBillingEndpoints
{
    /// <summary>
    /// Maps all Admin Billing endpoints under the admin route group.
    /// </summary>
    public static IEndpointRouteBuilder MapAdminBillingEndpoints(this IEndpointRouteBuilder group)
    {
        // GET /admin/v1/billing/invoices - List invoices (placeholder)
        group.MapGet("/billing/invoices", (ISender sender, CancellationToken ct) =>
        {
            // TODO: Implement GetAdminInvoicesQuery when billing invoice tracking is added
            return Task.FromResult(Results.Ok(new { message = "Billing invoices endpoint - not yet implemented" }));
        })
        .WithName("AdminV1_GetBillingInvoices")
        .WithSummary("List billing invoices")
        .WithDescription("Retrieves a list of billing invoices (placeholder - not yet implemented)")
        .WithOpenApi()
        .Produces<object>(StatusCodes.Status200OK);

        // POST /admin/v1/billing/issue - Issue invoice (placeholder)
        group.MapPost("/billing/issue", (ISender sender, CancellationToken ct) =>
        {
            // TODO: Implement IssueInvoiceCommand when billing invoice generation is added
            return Task.FromResult(Results.Ok(new { message = "Issue invoice endpoint - not yet implemented" }));
        })
        .WithName("AdminV1_IssueInvoice")
        .WithSummary("Issue invoice")
        .WithDescription("Issues a new invoice for a tenant (placeholder - not yet implemented)")
        .WithOpenApi()
        .Produces<object>(StatusCodes.Status200OK);

        return group;
    }
}

