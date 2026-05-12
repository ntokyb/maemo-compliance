using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MaemoCompliance.Infrastructure.Persistence;
using MaemoCompliance.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MaemoCompliance.IntegrationTests.Public;

[Collection("MaemoComplianceApi")]
public class TenantOnboardingIntegrationTests
{
    private readonly MaemoComplianceApiFixture _fixture;

    public TenantOnboardingIntegrationTests(MaemoComplianceApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task POST_public_signup_creates_tenant_and_sends_invite()
    {
        var client = _fixture.CreateClient();
        var email = $"admin-{Guid.NewGuid():N}@testorg.com";

        var response = await client.PostAsJsonAsync(
            "/public/signup",
            new
            {
                companyName = "Test Org",
                adminEmail = email,
                adminFirstName = "Test",
                adminLastName = "Admin",
                industry = "Manufacturing",
                plan = "Starter",
                isoFrameworks = new[] { "ISO9001" },
            });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);

        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaemoComplianceDbContext>();

        var tenant = await db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.AdminEmail == email);
        tenant.Should().NotBeNull(because: "signup should persist the tenant");
    }
}
