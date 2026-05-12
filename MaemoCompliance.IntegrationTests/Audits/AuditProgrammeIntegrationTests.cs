using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MaemoCompliance.Application.Audits.Dtos;
using MaemoCompliance.Domain.Audits;
using MaemoCompliance.IntegrationTests.Fixtures;
using Xunit;

namespace MaemoCompliance.IntegrationTests.Audits;

[Collection("MaemoComplianceApi")]
public class AuditProgrammeIntegrationTests : IAsyncLifetime
{
    private readonly MaemoComplianceApiFixture _fixture;
    private readonly Lazy<HttpClient> _client;

    public AuditProgrammeIntegrationTests(MaemoComplianceApiFixture fixture)
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

    public async Task InitializeAsync() => await _fixture.ResetTenantAuditDataAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task POST_create_programme_returns_success_and_persists()
    {
        var year = 2026;
        var post = await Client.PostAsJsonAsync("/api/audit-programmes", new
        {
            year,
            title = "Annual QMS Audit 2026",
            items = new[]
            {
                new
                {
                    processArea = "Document Control",
                    auditorName = "Jane",
                    plannedDate = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc),
                },
            },
        });
        post.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

        var dto = await Client.GetFromJsonAsync<AuditProgrammeDto>($"/api/audit-programmes/{year}");
        dto.Should().NotBeNull();
        dto!.Year.Should().Be(year);
        dto.Title.Should().Be("Annual QMS Audit 2026");
        dto.Items.Should().HaveCount(1);
        dto.Items[0].ProcessArea.Should().Be("Document Control");
    }

    [Fact]
    public async Task POST_link_audit_to_schedule_item()
    {
        var templateId = await _fixture.EnsureIntegrationAuditTemplateIdAsync();
        var runResponse = await Client.PostAsJsonAsync("/api/audits/runs", new { auditTemplateId = templateId });
        var runId = Guid.Parse(JsonSerializer.Deserialize<JsonElement>(await runResponse.Content.ReadAsStringAsync())
            .GetProperty("id").GetString()!);
        (await Client.PostAsync($"/api/audits/runs/{runId}/complete", null)).EnsureSuccessStatusCode();

        var year = 2027;
        var post = await Client.PostAsJsonAsync("/api/audit-programmes", new
        {
            year,
            title = "Programme with link",
            items = new[]
            {
                new
                {
                    processArea = "Document Control",
                    auditorName = "Jane",
                    plannedDate = new DateTime(2027, 7, 15, 0, 0, 0, DateTimeKind.Utc),
                },
            },
        });
        post.EnsureSuccessStatusCode();
        var created = JsonSerializer.Deserialize<JsonElement>(await post.Content.ReadAsStringAsync());
        var programmeId = Guid.Parse(created.GetProperty("id").GetString()!);

        var programme = await Client.GetFromJsonAsync<AuditProgrammeDto>($"/api/audit-programmes/{year}");
        programme.Should().NotBeNull();
        var itemId = programme!.Items[0].Id;

        var link = await Client.PostAsJsonAsync(
            $"/api/audit-programmes/{programmeId}/items/{itemId}/link-audit",
            new { auditId = runId });
        link.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.OK);

        var refreshed = await Client.GetFromJsonAsync<AuditProgrammeDto>($"/api/audit-programmes/{year}");
        refreshed.Should().NotBeNull();
        var item = refreshed!.Items.Should().ContainSingle().Subject;
        item.Status.Should().Be(AuditScheduleItemStatus.Complete);
        item.LinkedAuditId.Should().Be(runId);
    }
}
