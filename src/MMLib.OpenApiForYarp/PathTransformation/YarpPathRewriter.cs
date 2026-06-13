using Microsoft.OpenApi;
using Yarp.ReverseProxy.Configuration;

namespace MMLib.OpenApiForYarp.PathTransformation;

/// <summary>
/// Rewrites downstream OpenAPI path keys into the gateway-facing paths a client calls, by
/// inverting the YARP route match + path transforms.
/// </summary>
/// <remarks>
/// YARP transforms map an incoming gateway path to the forwarded downstream path. For
/// documentation we need the inverse: given a path the downstream service exposes, what path does
/// a client call on the gateway. We reason in terms of static prefixes — the gateway-facing
/// prefix from <c>Match.Path</c> and the downstream-facing prefix from the path transform — and
/// rebase each downstream path's remainder onto the gateway prefix, preserving path parameters
/// and catch-all remainders verbatim.
/// </remarks>
internal sealed class YarpPathRewriter
{
    private const string PathPattern = "PathPattern";
    private const string PathPrefix = "PathPrefix";
    private const string PathRemovePrefix = "PathRemovePrefix";
    private const string PathSet = "PathSet";

    /// <summary>
    /// Builds the downstream-path → gateway-path map for a single route. Downstream paths that are
    /// not reachable through this route (do not start with the downstream prefix) are omitted.
    /// </summary>
    public IReadOnlyDictionary<string, string> BuildPathMap(RouteConfig route, IEnumerable<string> downstreamPaths)
    {
        (string downstreamPrefix, string gatewayPrefix) = ResolvePrefixes(route);
        Dictionary<string, string> map = new(StringComparer.Ordinal);

        foreach (string downstreamPath in downstreamPaths)
        {
            if (TryRebase(downstreamPath, downstreamPrefix, gatewayPrefix, out string gatewayPath))
            {
                map[downstreamPath] = gatewayPath;
            }
        }

        return map;
    }

    /// <summary>
    /// Rewrites every path key of <paramref name="document"/> in place using the supplied routes.
    /// When several routes can serve a path, the most specific (longest downstream prefix) wins.
    /// Paths not served by any route are left unchanged. On a post-rewrite collision the first
    /// occurrence is kept.
    /// </summary>
    public void RewriteDocumentPaths(OpenApiDocument document, IReadOnlyList<RouteConfig> routes)
    {
        if (document.Paths is null || document.Paths.Count == 0 || routes.Count == 0)
        {
            return;
        }

        (string downstreamPrefix, string gatewayPrefix)[] resolved =
            [.. routes.Select(ResolvePrefixes)];

        List<KeyValuePair<string, IOpenApiPathItem>> entries = [.. document.Paths];
        document.Paths.Clear();

        foreach ((string originalPath, IOpenApiPathItem item) in entries)
        {
            string mapped = MapBestRoute(originalPath, resolved);
            if (!document.Paths.ContainsKey(mapped))
            {
                document.Paths[mapped] = item;
            }
        }
    }

    /// <summary>Computes the gateway path for a single downstream path against this route.</summary>
    public string? MapPath(RouteConfig route, string downstreamPath)
    {
        (string downstreamPrefix, string gatewayPrefix) = ResolvePrefixes(route);
        return TryRebase(downstreamPath, downstreamPrefix, gatewayPrefix, out string gatewayPath) ? gatewayPath : null;
    }

    private static string MapBestRoute(string downstreamPath, (string Downstream, string Gateway)[] resolved)
    {
        string? best = null;
        int bestPrefixLength = -1;

        foreach ((string downstreamPrefix, string gatewayPrefix) in resolved)
        {
            if (TryRebase(downstreamPath, downstreamPrefix, gatewayPrefix, out string gatewayPath)
                && downstreamPrefix.Length > bestPrefixLength)
            {
                best = gatewayPath;
                bestPrefixLength = downstreamPrefix.Length;
            }
        }

        return best ?? downstreamPath;
    }

    private static bool TryRebase(string downstreamPath, string downstreamPrefix, string gatewayPrefix, out string gatewayPath)
    {
        gatewayPath = string.Empty;

        if (downstreamPrefix.Length > 0)
        {
            bool onBoundary = downstreamPath.StartsWith(downstreamPrefix, StringComparison.Ordinal)
                && (downstreamPath.Length == downstreamPrefix.Length || downstreamPath[downstreamPrefix.Length] == '/');
            if (!onBoundary)
            {
                return false;
            }
        }

        string remainder = downstreamPath[downstreamPrefix.Length..];
        gatewayPath = YarpPathTemplate.Combine(gatewayPrefix, remainder);
        return true;
    }

    private static (string DownstreamPrefix, string GatewayPrefix) ResolvePrefixes(RouteConfig route)
    {
        string match = route.Match.Path ?? "/";
        (string? key, string? value) = FindPathTransform(route.Transforms);

        return key switch
        {
            PathPattern => (YarpPathTemplate.StaticPrefix(value), YarpPathTemplate.StaticPrefix(match)),
            PathSet => (YarpPathTemplate.StaticPrefix(value), YarpPathTemplate.StaticPrefix(match)),
            PathRemovePrefix => (string.Empty, YarpPathTemplate.StaticPrefix(value)),
            PathPrefix => (YarpPathTemplate.StaticPrefix(value), string.Empty),
            // No path transform: YARP forwards the full incoming path, so gateway path == downstream path.
            _ => (string.Empty, string.Empty),
        };
    }

    private static (string? Key, string? Value) FindPathTransform(IReadOnlyList<IReadOnlyDictionary<string, string>>? transforms)
    {
        if (transforms is null)
        {
            return (null, null);
        }

        foreach (IReadOnlyDictionary<string, string> transform in transforms)
        {
            foreach (string key in new[] { PathPattern, PathPrefix, PathRemovePrefix, PathSet })
            {
                foreach (KeyValuePair<string, string> entry in transform)
                {
                    if (string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase))
                    {
                        return (key, entry.Value);
                    }
                }
            }
        }

        return (null, null);
    }
}
