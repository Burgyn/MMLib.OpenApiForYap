using System.Text.RegularExpressions;
using Microsoft.OpenApi;
using MMLib.OpenApiForYarp.Abstractions;

namespace MMLib.OpenApiForYarp.Transformers;

/// <summary>
/// Built-in document transformer that filters paths: when
/// <see cref="Configuration.YarpOpenApiClusterOptions.AddOnlyPublishedPaths"/> is set, only paths
/// the gateway actually proxies are kept; then optional include/exclude regular expressions are
/// applied to the (already rewritten) gateway-facing paths.
/// </summary>
internal sealed class PublishedPathsFilterTransformer : IOpenApiDocumentTransformer
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        if (document.Paths is null || document.Paths.Count == 0)
        {
            return Task.CompletedTask;
        }

        if (context.Options.AddOnlyPublishedPaths)
        {
            FilterUnpublished(document, context);
        }

        ApplyRegexFilter(document, context.Options.IncludePaths, keepWhenMatch: true);
        ApplyRegexFilter(document, context.Options.ExcludePaths, keepWhenMatch: false);

        return Task.CompletedTask;
    }

    private static void FilterUnpublished(OpenApiDocument document, OpenApiDocumentTransformerContext context)
    {
        if (context.Items.TryGetValue(PathRewriteTransformer.PublishedPathsKey, out object? value)
            && value is HashSet<string> published)
        {
            foreach (string path in document.Paths.Keys.ToList())
            {
                if (!published.Contains(path))
                {
                    document.Paths.Remove(path);
                }
            }
        }
    }

    private static void ApplyRegexFilter(OpenApiDocument document, string[]? patterns, bool keepWhenMatch)
    {
        if (patterns is not { Length: > 0 })
        {
            return;
        }

        Regex[] regexes = [.. patterns.Select(p => new Regex(p, RegexOptions.None, RegexTimeout))];

        foreach (string path in document.Paths.Keys.ToList())
        {
            bool matches = regexes.Any(r => r.IsMatch(path));
            bool remove = keepWhenMatch ? !matches : matches;
            if (remove)
            {
                document.Paths.Remove(path);
            }
        }
    }
}
