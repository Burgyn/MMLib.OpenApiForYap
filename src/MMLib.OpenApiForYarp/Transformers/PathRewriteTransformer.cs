using Microsoft.OpenApi;
using MMLib.OpenApiForYarp.Abstractions;
using MMLib.OpenApiForYarp.PathTransformation;

namespace MMLib.OpenApiForYarp.Transformers;

/// <summary>
/// Built-in document transformer that rewrites downstream path keys into the gateway-facing paths,
/// based on the YARP route match and path transforms. Records the set of published gateway paths
/// in <see cref="OpenApiTransformerContext.Items"/> for the published-paths filter.
/// </summary>
internal sealed class PathRewriteTransformer : IOpenApiDocumentTransformer
{
    /// <summary>Items key under which the set of published gateway paths is stored.</summary>
    public const string PublishedPathsKey = "MMLib.OpenApiForYarp.PublishedPaths";

    private readonly YarpPathRewriter _rewriter = new();

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        HashSet<string> published = new(StringComparer.Ordinal);

        if (document.Paths is { Count: > 0 })
        {
            foreach (string downstreamPath in document.Paths.Keys.ToList())
            {
                foreach (var route in context.Routes)
                {
                    string? gatewayPath = _rewriter.MapPath(route, downstreamPath);
                    if (gatewayPath is not null)
                    {
                        published.Add(gatewayPath);
                    }
                }
            }
        }

        _rewriter.RewriteDocumentPaths(document, context.Routes);
        context.Items[PublishedPathsKey] = published;

        return Task.CompletedTask;
    }
}
