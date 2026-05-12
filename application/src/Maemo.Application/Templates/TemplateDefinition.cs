namespace Maemo.Application.Templates;

/// <summary>
/// Definition of a template available in the template library.
/// </summary>
public record TemplateDefinition
{
    public string Id { get; init; } = null!;
    public string Standard { get; init; } = null!;
    public string Category { get; init; } = null!;
    public string Type { get; init; } = null!;
    public string Title { get; init; } = null!;
    public string ShortCode { get; init; } = null!;
    public string TargetModule { get; init; } = null!;
    public string Description { get; init; } = null!;
    public string JsonPath { get; init; } = null!;
    public string MarkdownPath { get; init; } = null!;
}

