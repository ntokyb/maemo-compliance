namespace Maemo.Application.Documents.Dtos;

public class CreateNewDocumentVersionRequest
{
    public string Title { get; set; } = null!;
    public string? Category { get; set; }
    public string? Department { get; set; }
    public string? OwnerUserId { get; set; }
    public DateTime ReviewDate { get; set; }
    public string? Comments { get; set; }
}

