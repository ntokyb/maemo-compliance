using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Maemo.Application.Ncrs.Commands;
using Maemo.Application.Ncrs.Dtos;
using Maemo.Domain.Ncrs;
using Maemo.IntegrationTests.Fixtures;
using Xunit;

namespace Maemo.IntegrationTests.Ncrs;

[Collection("MaemoApi")]
public class NcrsApiTests : IAsyncLifetime
{
    private readonly MaemoApiFixture _fixture;
    private readonly Lazy<HttpClient> _client;

    public NcrsApiTests(MaemoApiFixture fixture)
    {
        _fixture = fixture;
        _client = new Lazy<HttpClient>(() =>
        {
            var client = fixture.CreateClient();
            client.DefaultRequestHeaders.Add("X-Tenant-Id", MaemoApiFixture.TestTenantId.ToString());
            return client;
        });
    }

    private HttpClient Client => _client.Value;

    public Task InitializeAsync() => _fixture.ResetTenantNcrsAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateNcr_ThenUpdateNcr_ReturnsUpdatedFields()
    {
        var createResponse = await Client.PostAsJsonAsync("/api/ncrs", new CreateNcrCommand
        {
            Title = "Original title",
            Description = "Original description",
            Severity = NcrSeverity.Low,
            DueDate = DateTime.UtcNow.AddDays(14),
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createJson = await createResponse.Content.ReadAsStringAsync();
        var id = Guid.Parse(JsonSerializer.Deserialize<JsonElement>(createJson).GetProperty("id").GetString()!);

        var updateRequest = new UpdateNcrRequest
        {
            Title = "Updated title",
            Description = "Updated description",
            Severity = NcrSeverity.High,
            DueDate = DateTime.UtcNow.AddDays(30),
            Category = NcrCategory.Process,
            RootCause = "Updated root cause",
            CorrectiveAction = "Fix it",
            EscalationLevel = 1,
        };

        var updateResponse = await Client.PutAsJsonAsync($"/api/ncrs/{id}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<NcrDto>();
        updated.Should().NotBeNull();
        updated!.Title.Should().Be(updateRequest.Title);
        updated.RootCause.Should().Be(updateRequest.RootCause);

        var getResponse = await Client.GetAsync($"/api/ncrs/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var ncr = await getResponse.Content.ReadFromJsonAsync<NcrDto>();
        ncr.Should().NotBeNull();
        ncr!.Title.Should().Be(updateRequest.Title);
        ncr.RootCause.Should().Be(updateRequest.RootCause);
    }

    [Fact]
    public async Task CreateNcr_ThenDeleteNcr_ReturnsNotFound()
    {
        var createResponse = await Client.PostAsJsonAsync("/api/ncrs", new CreateNcrCommand
        {
            Title = "To delete",
            Description = "Desc",
            Severity = NcrSeverity.Medium,
            DueDate = DateTime.UtcNow.AddDays(7),
        });
        createResponse.EnsureSuccessStatusCode();
        var createJson = await createResponse.Content.ReadAsStringAsync();
        var id = Guid.Parse(JsonSerializer.Deserialize<JsonElement>(createJson).GetProperty("id").GetString()!);

        var deleteResponse = await Client.DeleteAsync($"/api/ncrs/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await Client.GetAsync($"/api/ncrs/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteClosedNcr_Returns409()
    {
        var createResponse = await Client.PostAsJsonAsync("/api/ncrs", new CreateNcrCommand
        {
            Title = "Close me",
            Description = "Desc",
            Severity = NcrSeverity.Medium,
            DueDate = DateTime.UtcNow.AddDays(7),
        });
        createResponse.EnsureSuccessStatusCode();
        var createJson = await createResponse.Content.ReadAsStringAsync();
        var id = Guid.Parse(JsonSerializer.Deserialize<JsonElement>(createJson).GetProperty("id").GetString()!);

        var statusResponse = await Client.PutAsJsonAsync($"/api/ncrs/{id}/status", new
        {
            status = NcrStatus.Closed,
            dueDate = (DateTime?)null,
            closedAt = (DateTime?)null,
        });
        statusResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var deleteResponse = await Client.DeleteAsync($"/api/ncrs/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
