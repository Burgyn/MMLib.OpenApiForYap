using Microsoft.OpenApi;
using MMLib.OpenApiForYarp.Abstractions;

namespace MMLib.OpenApiForYarp.Transformers;

/// <summary>
/// Built-in document transformer that propagates a downstream document's security schemes into the
/// aggregated output. Downstream schemes are kept as-is; when a per-cluster
/// <see cref="Configuration.YarpOpenApiClusterOptions.SecurityScheme"/> override is set, only that
/// scheme is retained. Cross-service deduplication happens in the document merger.
/// </summary>
internal sealed class SecurityPropagationTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        string? requested = context.Options.SecurityScheme;
        if (string.IsNullOrEmpty(requested))
        {
            // Identity: keep whatever security schemes the downstream document declared.
            return Task.CompletedTask;
        }

        IDictionary<string, IOpenApiSecurityScheme>? schemes = document.Components?.SecuritySchemes;
        if (schemes is null || schemes.Count == 0)
        {
            return Task.CompletedTask;
        }

        foreach (string name in schemes.Keys.ToList())
        {
            if (!string.Equals(name, requested, StringComparison.Ordinal))
            {
                schemes.Remove(name);
            }
        }

        return Task.CompletedTask;
    }
}
