using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Maemo.Application.Documents.Dtos;
using Maemo.Domain.Documents;
using Maemo.IntegrationTests.Fixtures;
using Xunit;

namespace Maemo.IntegrationTests.Documents;

public class DocumentsApiTests : IClassFixture<MaemoApiFixture>, IAsyncLifetime
{
    private readonly MaemoApiFixture _fixture;
    private readonly Lazy<HttpClient> _client;

    public DocumentsApiTests(MaemoApiFixture fixture)
    {
        _fixture = fixture;
        // Lazy: first use is in test methods, after fixture IAsyncLifetime has started Postgres and migrated.
        _client = new Lazy<HttpClient>(() =>
        {
            var client = fixture.CreateClient();
            client.DefaultRequestHeaders.Add("X-Tenant-Id", MaemoApiFixture.TestTenantId.ToString());
            return client;
        });
    }

    private HttpClient Client => _client.Value;

    public Task InitializeAsync() => _fixture.ResetTenantDocumentsAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetDocuments_WhenNoDocumentsExist_ReturnsEmptyList()
    {
        // Act
        var response = await Client.GetAsync("/api/documents");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var documents = await response.Content.ReadFromJsonAsync<List<DocumentDto>>();
        documents.Should().NotBeNull();
        documents.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateDocument_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new CreateDocumentRequest
        {
            Title = "Test Document",
            Category = "Policy",
            Department = "IT",
            ReviewDate = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/documents", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var location = response.Headers.Location?.ToString();
        location.Should().NotBeNull();
        location.Should().Contain("/api/documents/");

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        result.TryGetProperty("id", out var idElement).Should().BeTrue();
        Guid.TryParse(idElement.GetString(), out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateDocument_ThenGetDocuments_ReturnsCreatedDocument()
    {
        // Arrange
        var createRequest = new CreateDocumentRequest
        {
            Title = "Integration Test Document",
            Category = "Procedure",
            Department = "HR",
            ReviewDate = DateTime.UtcNow.AddDays(60)
        };

        // Create document
        var createResponse = await Client.PostAsJsonAsync("/api/documents", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<JsonElement>(createContent);
        var documentId = Guid.Parse(createResult.GetProperty("id").GetString()!);

        // Act - Get all documents
        var getResponse = await Client.GetAsync("/api/documents");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var documents = await getResponse.Content.ReadFromJsonAsync<List<DocumentDto>>();
        documents.Should().NotBeNull();
        documents.Should().HaveCount(1);
        documents![0].Id.Should().Be(documentId);
        documents[0].Title.Should().Be(createRequest.Title);
        documents[0].Category.Should().Be(createRequest.Category);
        documents[0].Department.Should().Be(createRequest.Department);
        documents[0].Status.Should().Be(DocumentStatus.Draft);
    }

    [Fact]
    public async Task UpdateDocument_WithValidRequest_ReturnsNoContent()
    {
        // Arrange - Create a document first
        var createRequest = new CreateDocumentRequest
        {
            Title = "Document to Update",
            Category = "Policy",
            ReviewDate = DateTime.UtcNow.AddDays(30)
        };

        var createResponse = await Client.PostAsJsonAsync("/api/documents", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<JsonElement>(createContent);
        var documentId = Guid.Parse(createResult.GetProperty("id").GetString()!);

        // Act - Update the document
        var updateRequest = new UpdateDocumentRequest
        {
            Title = "Updated Document Title",
            Category = "Updated Category",
            Department = "Finance",
            ReviewDate = DateTime.UtcNow.AddDays(90),
            Status = DocumentStatus.Active
        };

        var updateResponse = await Client.PutAsJsonAsync($"/api/documents/{documentId}", updateRequest);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify the update
        var getResponse = await Client.GetAsync($"/api/documents/{documentId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await getResponse.Content.ReadFromJsonAsync<DocumentDto>();
        document.Should().NotBeNull();
        document!.Title.Should().Be(updateRequest.Title);
        document.Category.Should().Be(updateRequest.Category);
        document.Department.Should().Be(updateRequest.Department);
        document.Status.Should().Be(updateRequest.Status);
    }

    [Fact]
    public async Task GetDocumentById_WhenDocumentExists_ReturnsDocument()
    {
        // Arrange - Create a document
        var createRequest = new CreateDocumentRequest
        {
            Title = "Document to Retrieve",
            ReviewDate = DateTime.UtcNow.AddDays(30)
        };

        var createResponse = await Client.PostAsJsonAsync("/api/documents", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<JsonElement>(createContent);
        var documentId = Guid.Parse(createResult.GetProperty("id").GetString()!);

        // Act
        var response = await Client.GetAsync($"/api/documents/{documentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await response.Content.ReadFromJsonAsync<DocumentDto>();
        document.Should().NotBeNull();
        document!.Id.Should().Be(documentId);
        document.Title.Should().Be(createRequest.Title);
    }

    [Fact]
    public async Task GetDocumentById_WhenDocumentDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/documents/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

