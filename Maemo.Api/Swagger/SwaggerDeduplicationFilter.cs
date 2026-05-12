using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace Maemo.Api.Swagger;

/// <summary>
/// Swagger document filter to deduplicate operations that might have slipped through ResolveConflictingActions.
/// This is a safety net to prevent "Sequence contains more than one matching element" errors.
/// </summary>
public class SwaggerDeduplicationFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Deduplicate operations by path and method
        foreach (var path in swaggerDoc.Paths.ToList())
        {
            var operations = path.Value.Operations.ToList();
            
            // Check for duplicate HTTP methods (shouldn't happen, but safety check)
            var methods = operations.Select(o => o.Key).ToList();
            var duplicateMethods = methods.GroupBy(m => m)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            
            if (duplicateMethods.Any())
            {
                // If duplicates found, keep only the first occurrence
                // Prefer GET, then POST, then PUT, then DELETE, then PATCH
                var methodPriority = new Dictionary<OperationType, int>
                {
                    { OperationType.Get, 1 },
                    { OperationType.Post, 2 },
                    { OperationType.Put, 3 },
                    { OperationType.Delete, 4 },
                    { OperationType.Patch, 5 },
                    { OperationType.Head, 6 },
                    { OperationType.Options, 7 },
                    { OperationType.Trace, 8 }
                };
                
                foreach (var duplicateMethod in duplicateMethods)
                {
                    var duplicates = operations.Where(o => o.Key == duplicateMethod).ToList();
                    if (duplicates.Count > 1)
                    {
                        // Keep the first one, remove others
                        var toKeep = duplicates.First();
                        foreach (var toRemove in duplicates.Skip(1))
                        {
                            path.Value.Operations.Remove(toRemove.Key);
                        }
                    }
                }
            }
        }
    }
}
