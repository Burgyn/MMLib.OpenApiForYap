using Yarp.ReverseProxy.Configuration;

namespace MMLib.OpenApiForYarp.Yarp;

/// <summary>A cluster together with the routes that target it, read from the live YARP config.</summary>
/// <param name="ClusterId">The YARP cluster id.</param>
/// <param name="Routes">All routes whose <c>ClusterId</c> equals <paramref name="ClusterId"/>.</param>
/// <param name="Cluster">The cluster configuration (destinations, etc.).</param>
internal sealed record YarpClusterDescriptor(
    string ClusterId,
    IReadOnlyList<RouteConfig> Routes,
    ClusterConfig Cluster);

/// <summary>
/// Reads the live YARP configuration (via <see cref="IProxyConfigProvider"/>) and exposes it as a
/// per-cluster view: the cluster plus the routes that target it. Always reads a fresh snapshot so
/// configuration changes are observed without caching stale state.
/// </summary>
internal sealed class YarpConfigSource(IEnumerable<IProxyConfigProvider> providers)
{
    private readonly IEnumerable<IProxyConfigProvider> _providers = providers;

    /// <summary>Returns the ids of every configured cluster.</summary>
    public IReadOnlyList<string> GetClusterIds()
    {
        (IReadOnlyDictionary<string, ClusterConfig> clusters, _) = Snapshot();
        return [.. clusters.Keys];
    }

    /// <summary>Looks up a cluster and the routes targeting it.</summary>
    public bool TryGetCluster(string clusterId, out YarpClusterDescriptor descriptor)
    {
        (IReadOnlyDictionary<string, ClusterConfig> clusters, ILookup<string, RouteConfig> routesByCluster) = Snapshot();

        if (clusters.TryGetValue(clusterId, out ClusterConfig? cluster))
        {
            descriptor = new YarpClusterDescriptor(cluster.ClusterId, [.. routesByCluster[cluster.ClusterId]], cluster);
            return true;
        }

        descriptor = null!;
        return false;
    }

    private (IReadOnlyDictionary<string, ClusterConfig> Clusters, ILookup<string, RouteConfig> RoutesByCluster) Snapshot()
    {
        Dictionary<string, ClusterConfig> clusters = new(StringComparer.OrdinalIgnoreCase);
        List<RouteConfig> routes = [];

        foreach (IProxyConfigProvider provider in _providers)
        {
            IProxyConfig config = provider.GetConfig();
            foreach (ClusterConfig cluster in config.Clusters)
            {
                clusters[cluster.ClusterId] = cluster;
            }

            routes.AddRange(config.Routes);
        }

        ILookup<string, RouteConfig> routesByCluster = routes
            .Where(route => route.ClusterId is not null)
            .ToLookup(route => route.ClusterId!, StringComparer.OrdinalIgnoreCase);

        return (clusters, routesByCluster);
    }
}
