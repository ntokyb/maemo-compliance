using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MaemoCompliance.Application.Documents.Dtos;
using MaemoCompliance.Domain.Documents;
using MaemoCompliance.IntegrationTests.Fixtures;
using Xunit;

namespace MaemoCompliance.IntegrationTests.Documents;

[Collection("MaemoComplianceApi")]
public class DocumentApprovalIntegrationTests : IAsyncLifetime
{
    private readonly MaemoComplianceApiFixture _fixture;
    private readonly Lazy<HttpClient> _client;

    public DocumentApprovalIntegrationTests(MaemoComplianceApiFixture fixture)
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

    public Task InitializeAsync() => _fixture.ResetTenantDocumentsAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task POST_submit_for_review_returns_success_and_status_changes()
    {
        var docId = await CreateDraftDocumentWithFileAsync();

        var submit = await Client.PostAsync($"/api/documents/{docId}/submit-for-review", null);
        submit.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.OK);

        var get = await Client.GetFromJsonAsync<DocumentDto>($"/api/documents/{docId}");
        get.Should().NotBeNull();
        get!.Status.Should().Be(DocumentStatus.UnderReview);
        get.SubmittedForReviewAt.Should().NotBeNull();
    }

    [Fact]
    public async Task POST_approve_returns_success_and_status_changes()
    {
        var docId = await CreateDraftDocumentWithFileAsync();
        (await Client.PostAsync($"/api/documents/{docId}/submit-for-review", null)).EnsureSuccessStatusCode();

        var approve = await Client.PostAsJsonAsync($"/api/documents/{docId}/approve", new { approverName = "Integration Approver" });
        approve.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.OK);

        var get = await Client.GetFromJsonAsync<DocumentDto>($"/api/documents/{docId}");
        get.Should().NotBeNull();
        get!.WorkflowState.Should().Be(DocumentWorkflowState.Approved);
        get.Status.Should().Be(DocumentStatus.Approved);
        get.ApprovedBy.Should().Be("Integration Approver");
    }

    [Fact]
    public async Task Non_approver_cannot_approve_document()
    {
        var docId = await CreateDraftDocumentWithFileAsync();
        (await Client.PostAsync($"/api/documents/{docId}/submit-for-review", null)).EnsureSuccessStatusCode();

        var noApproverClient = _fixture.CreateClient();
        noApproverClient.DefaultRequestHeaders.Add("X-Tenant-Id", MaemoComplianceApiFixture.TestTenantId.ToString());
        noApproverClient.DefaultRequestHeaders.Add("X-Test-Roles", "TenantAdmin");

        var approve = await noApproverClient.PostAsJsonAsync($"/api/documents/{docId}/approve", new { });
        approve.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task POST_return_for_revision_returns_success()
    {
        var docId = await CreateDraftDocumentWithFileAsync();
        (await Client.PostAsync($"/api/documents/{docId}/submit-for-review", null)).EnsureSuccessStatusCode();

        var ret = await Client.PostAsJsonAsync($"/api/documents/{docId}/return-for-revision", new { reason = "Missing signature" });
        ret.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.OK);

        var get = await Client.GetFromJsonAsync<DocumentDto>($"/api/documents/{docId}");
        get.Should().NotBeNull();
        get!.Status.Should().Be(DocumentStatus.Draft);
        get.RejectedReason.Should().Be("Missing signature");
    }

    private async Task<Guid> CreateDraftDocumentWithFileAsync()
    {
        var createRequest = new CreateDocumentRequest
        {
            Title = "Approval flow doc",
            Category = "Policy",
            ReviewDate = DateTime.UtcNow.AddDays(30),
        };

        var createResponse = await Client.PostAsJsonAsync("/api/documents", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await createResponse.Content.ReadAsStringAsync();
        var id = Guid.Parse(JsonSerializer.Deserialize<JsonElement>(json).GetProperty("id").GetString()!);

        await _fixture.SetDocumentStorageLocationAsync(id, $"integration-test/{id}/file.bin");
        return id;
    }
}
