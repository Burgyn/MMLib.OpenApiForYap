using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace MMLib.OpenApiForYarp.Tests;

internal sealed class FakeProxyConfig : IProxyConfig
{
    public IReadOnlyList<RouteConfig> Routes { get; init; } = [];

    public IReadOnlyList<ClusterConfig> Clusters { get; init; } = [];

    public IChangeToken ChangeToken { get; } = new CancellationChangeToken(CancellationToken.None);
}

internal sealed class FakeProxyConfigProvider(IProxyConfig config) : IProxyConfigProvider
{
    public IProxyConfig GetConfig() => config;
}

internal static class FakeYarp
{
    public static RouteConfig Route(string routeId, string clusterId, string matchPath, params (string Key, string Value)[] transforms)
        => new()
        {
            RouteId = routeId,
            ClusterId = clusterId,
            Match = new RouteMatch { Path = matchPath },
            Transforms = transforms.Length == 0
                ? null
                : [.. transforms.Select(t => new Dictionary<string, string> { [t.Key] = t.Value })],
        };

    public static ClusterConfig Cluster(string clusterId, string address)
        => new()
        {
            ClusterId = clusterId,
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["default"] = new DestinationConfig { Address = address },
            },
        };

    public static FakeProxyConfigProvider Provider(IEnumerable<RouteConfig> routes, IEnumerable<ClusterConfig> clusters)
        => new(new FakeProxyConfig { Routes = [.. routes], Clusters = [.. clusters] });
}
