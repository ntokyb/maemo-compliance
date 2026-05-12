using Maemo.Engine.Client;
using Maemo.Engine.Client.Models;

// Initialize the client with your API base URL and API key
var options = new MaemoEngineClientOptions
{
    BaseUrl = "https://api.maemo.com", // Replace with your Maemo API base URL
    ApiKey = "your-api-key-here" // Replace with your API key
};

using var client = new MaemoEngineClient(options);

try
{
    // Example 1: Create a new document
    Console.WriteLine("Creating a new document...");
    var createDocumentRequest = new CreateDocumentRequest
    {
        Title = "Sample Compliance Document",
        Category = "Policy",
        Department = "IT",
        OwnerUserId = "user-123",
        ReviewDate = DateTime.UtcNow.AddMonths(6)
    };

    var documentId = await client.Documents.CreateDocumentAsync(createDocumentRequest);
    Console.WriteLine($"Document created with ID: {documentId}");

    // Example 2: Retrieve the document
    Console.WriteLine("\nRetrieving the document...");
    var document = await client.Documents.GetDocumentAsync(documentId);
    if (document != null)
    {
        Console.WriteLine($"Document Title: {document.Title}");
        Console.WriteLine($"Document Status: {document.Status}");
        Console.WriteLine($"Document Version: {document.Version}");
    }

    // Example 3: List all documents
    Console.WriteLine("\nListing all documents...");
    var documents = await client.Documents.GetDocumentsAsync();
    Console.WriteLine($"Found {documents.Count} document(s)");

    // Example 4: Create an NCR
    Console.WriteLine("\nCreating a new NCR...");
    var createNcrRequest = new CreateNcrRequest
    {
        Title = "Sample Non-Conformance",
        Description = "This is a sample NCR for demonstration purposes",
        Department = "Quality",
        OwnerUserId = "user-456",
        Severity = NcrSeverity.Medium,
        Category = NcrCategory.Process,
        DueDate = DateTime.UtcNow.AddDays(30)
    };

    var ncrId = await client.Ncr.CreateNcrAsync(createNcrRequest);
    Console.WriteLine($"NCR created with ID: {ncrId}");

    // Example 5: Create a risk
    Console.WriteLine("\nCreating a new risk...");
    var createRiskRequest = new CreateRiskRequest
    {
        Title = "Sample Risk",
        Description = "This is a sample risk for demonstration purposes",
        Category = RiskCategory.Compliance,
        InherentLikelihood = 3,
        InherentImpact = 4,
        ResidualLikelihood = 2,
        ResidualImpact = 3,
        OwnerUserId = "user-789",
        Status = RiskStatus.Identified
    };

    var riskId = await client.Risks.CreateRiskAsync(createRiskRequest);
    Console.WriteLine($"Risk created with ID: {riskId}");

    // Example 6: List audit templates
    Console.WriteLine("\nListing audit templates...");
    var templates = await client.Audit.GetTemplatesAsync();
    Console.WriteLine($"Found {templates.Count} audit template(s)");

    if (templates.Count > 0)
    {
        // Example 7: Start an audit run
        Console.WriteLine("\nStarting an audit run...");
        var auditRunId = await client.Audit.StartRunAsync(templates[0].Id, "auditor-123");
        Console.WriteLine($"Audit run started with ID: {auditRunId}");
    }

    Console.WriteLine("\nAll operations completed successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}
