namespace Maemo.Application.Documents.Dtos;

/// <summary>
/// DTO for document version information.
/// </summary>
public class DocumentVersionDto
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string FileName { get; set; } = null!;
    public string StorageLocation { get; set; } = null!;
    public string? UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? Comment { get; set; }
    public bool IsLatest { get; set; }
}

