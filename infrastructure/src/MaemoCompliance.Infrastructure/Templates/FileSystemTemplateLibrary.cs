using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Templates;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

namespace MaemoCompliance.Infrastructure.Templates;

/// <summary>
/// File system-based implementation of the template library.
/// Discovers templates from the templates/backend directory structure.
/// </summary>
public class FileSystemTemplateLibrary : ITemplateLibrary
{
    private readonly IHostEnvironment _environment;
    private readonly IMemoryCache _cache;
    private const string CacheKey = "TemplateLibrary_AllTemplates";
    private const string CacheDuration = "00:10:00"; // Cache for 10 minutes

    public FileSystemTemplateLibrary(
        IHostEnvironment environment,
        IMemoryCache cache)
    {
        _environment = environment;
        _cache = cache;
    }

    public async Task<IReadOnlyList<TemplateDefinition>> GetTemplatesAsync(
        string? standard = null,
        string? module = null,
        CancellationToken cancellationToken = default)
    {
        var templates = await GetAllTemplatesAsync(cancellationToken);

        var filtered = templates.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(standard))
        {
            filtered = filtered.Where(t => 
                t.Standard.Equals(standard, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(module))
        {
            filtered = filtered.Where(t => 
                t.TargetModule.Equals(module, StringComparison.OrdinalIgnoreCase));
        }

        return filtered.ToList();
    }

    public async Task<TemplateDefinition?> GetTemplateByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var templates = await GetAllTemplatesAsync(cancellationToken);
        return templates.FirstOrDefault(t => 
            t.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<IReadOnlyList<TemplateDefinition>> GetAllTemplatesAsync(
        CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(CacheKey, out IReadOnlyList<TemplateDefinition>? cachedTemplates))
        {
            return cachedTemplates ?? Array.Empty<TemplateDefinition>();
        }

        var templates = new List<TemplateDefinition>();
        var templatesRoot = Path.Combine(_environment.ContentRootPath, "templates", "backend");

        if (!Directory.Exists(templatesRoot))
        {
            return Array.Empty<TemplateDefinition>();
        }

        // Find all *.template.json files recursively
        var jsonFiles = Directory.EnumerateFiles(
            templatesRoot,
            "*.template.json",
            SearchOption.AllDirectories);

        foreach (var jsonFile in jsonFiles)
        {
            try
            {
                var template = await LoadTemplateFromFileAsync(jsonFile, cancellationToken);
                if (template != null)
                {
                    templates.Add(template);
                }
            }
            catch (Exception ex)
            {
                // Log error but continue processing other templates
                // In production, you might want to use ILogger here
                Console.WriteLine($"Error loading template from {jsonFile}: {ex.Message}");
            }
        }

        // Cache the results
        _cache.Set(CacheKey, templates, TimeSpan.Parse(CacheDuration));

        return templates;
    }

    private async Task<TemplateDefinition?> LoadTemplateFromFileAsync(
        string jsonFilePath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(jsonFilePath))
        {
            return null;
        }

        var jsonContent = await File.ReadAllTextAsync(jsonFilePath, cancellationToken);
        var metadata = JsonSerializer.Deserialize<TemplateMetadata>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (metadata == null)
        {
            return null;
        }

        // Derive paths relative to ContentRootPath
        var contentRoot = _environment.ContentRootPath;
        var jsonPath = Path.GetRelativePath(contentRoot, jsonFilePath);
        var markdownPath = jsonPath.Replace(".template.json", ".template.md");

        // Extract standard and category from directory structure
        // e.g., templates/backend/iso9001/documents/file.template.json
        var relativePath = Path.GetRelativePath(
            Path.Combine(contentRoot, "templates", "backend"),
            jsonFilePath);
        var pathParts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        
        var standard = pathParts.Length > 0 ? pathParts[0] : metadata.Standard ?? "Unknown";
        var category = pathParts.Length > 1 ? pathParts[1] : metadata.Category ?? "General";

        return new TemplateDefinition
        {
            Id = metadata.Id ?? Path.GetFileNameWithoutExtension(jsonFilePath),
            Standard = standard,
            Category = category,
            Type = metadata.Type ?? "Document",
            Title = metadata.Title ?? Path.GetFileNameWithoutExtension(jsonFilePath),
            ShortCode = metadata.ShortCode ?? string.Empty,
            TargetModule = metadata.TargetModule ?? "Documents",
            Description = metadata.Description ?? string.Empty,
            JsonPath = jsonPath.Replace('\\', '/'), // Normalize to forward slashes
            MarkdownPath = markdownPath.Replace('\\', '/')
        };
    }

    private class TemplateMetadata
    {
        public string? Id { get; set; }
        public string? Standard { get; set; }
        public string? Category { get; set; }
        public string? Type { get; set; }
        public string? Title { get; set; }
        public string? ShortCode { get; set; }
        public string? TargetModule { get; set; }
        public string? Description { get; set; }
    }
}

