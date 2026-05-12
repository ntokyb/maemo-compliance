using MaemoCompliance.Application.Templates;

namespace MaemoCompliance.Application.Common;

/// <summary>
/// Service for accessing the template library.
/// </summary>
public interface ITemplateLibrary
{
    /// <summary>
    /// Gets all available templates, optionally filtered by standard and/or module.
    /// </summary>
    Task<IReadOnlyList<TemplateDefinition>> GetTemplatesAsync(
        string? standard = null,
        string? module = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific template by its ID.
    /// </summary>
    Task<TemplateDefinition?> GetTemplateByIdAsync(
        string id,
        CancellationToken cancellationToken = default);
}

