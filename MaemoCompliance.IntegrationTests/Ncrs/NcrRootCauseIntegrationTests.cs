using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MaemoCompliance.Application.Audits.Dtos;
using MaemoCompliance.Application.Ncrs.Commands;
using MaemoCompliance.Application.Ncrs.Dtos;
using MaemoCompliance.Domain.Ncrs;
using MaemoCompliance.Infrastructure.Persistence;
using MaemoCompliance.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MaemoCompliance.IntegrationTests.Ncrs;

[Collection("MaemoComplianceApi")]
public class NcrRootCauseIntegrationTests : IAsyncLifetime
{
    private readonly MaemoComplianceApiFixture _fixture;
    private readonly Lazy<HttpClient> _client;

    public NcrRootCauseIntegrationTests(MaemoComplianceApiFixture fixture)
    {
        _fixture = fixture;
        _client = new Lazy<HttpClient>(() =>
        {
            var client = fixture.CreateClient();
            client.DefaultRequestHeaders.Add("X-Tenant-Id", MaemoComplianceApiFixture.TestTenantId.ToString());
            return client;
        });
    }

    private HttpClient Client => _client.Value;

    public async Task InitializeAsync()
    {
        await _fixture.ResetTenantNcrsAsync();
        await _fixture.ResetTenantAuditDataAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task PUT_ncr_root_cause_persists_all_fields()
    {
        var createResponse = await Client.PostAsJsonAsync("/api/ncrs", new CreateNcrCommand
        {
            Title = "RC int",
            Description = "d",
            Severity = NcrSeverity.Medium,
            DueDate = DateTime.UtcNow.AddDays(7),
        });
        createResponse.EnsureSuccessStatusCode();
        var id = Guid.Parse(JsonSerializer.Deserialize<JsonElement>(await createResponse.Content.ReadAsStringAsync())
            .GetProperty("id").GetString()!);

        var put = await Client.PutAsJsonAsync($"/api/ncrs/{id}/root-cause", new
        {
            rootCauseMethod = "5-Why",
            rootCause = "Training gap",
            correctiveActionPlan = "Retrain team",
            correctiveActionOwner = "John",
            correctiveActionDueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
        });
        put.StatusCode.Should().Be(HttpStatusCode.OK);

        var entity = await _fixture.GetNcrEntityAsync(id);
        entity.Should().NotBeNull();
        entity!.RootCause.Should().Be("Training gap");
        entity.RootCauseMethod.Should().Be("5-Why");
        entity.CorrectiveActionPlan.Should().Be("Retrain team");
    }

    [Fact]
    public async Task POST_create_ncr_from_audit_finding_links_correctly()
    {
        var templateId = await _fixture.EnsureIntegrationAuditTemplateIdAsync();

        var runResponse = await Client.PostAsJsonAsync("/api/audits/runs", new { auditTemplateId = templateId });
        runResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var runId = Guid.Parse(JsonSerializer.Deserialize<JsonElement>(await runResponse.Content.ReadAsStringAsync())
            .GetProperty("id").GetString()!);

        (await Client.PostAsync($"/api/audits/runs/{runId}/complete", null)).EnsureSuccessStatusCode();

        var findingResponse = await Client.PostAsJsonAsync($"/api/audits/runs/{runId}/findings", new { title = "Minor NC" });
        findingResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdFinding = await findingResponse.Content.ReadFromJsonAsync<AuditFindingDto>();
        createdFinding.Should().NotBeNull();
        var findingId = createdFinding!.Id;

        var ncrRes = await Client.PostAsync($"/api/audits/runs/{runId}/findings/{findingId}/create-ncr", null);
        ncrRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var ncrJson = await ncrRes.Content.ReadAsStringAsync();
        var ncrId = Guid.Parse(JsonSerializer.Deserialize<JsonElement>(ncrJson).GetProperty("ncrId").GetString()!);

        var ncr = await Client.GetFromJsonAsync<NcrDto>($"/api/ncrs/{ncrId}");
        ncr.Should().NotBeNull();
        ncr!.LinkedAuditFindingId.Should().Be(findingId);

        var findingGet = await Client.GetFromJsonAsync<AuditFindingDto>($"/api/audits/runs/{runId}/findings/{findingId}");
        findingGet.Should().NotBeNull();
        findingGet!.LinkedNcrId.Should().Be(ncrId);
    }
}
