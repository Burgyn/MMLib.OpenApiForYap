using Microsoft.OpenApi;
using MMLib.OpenApiForYarp.Abstractions;
using MMLib.OpenApiForYarp.Configuration;
using Yarp.ReverseProxy.Configuration;

namespace MMLib.OpenApiForYarp.Tests;

internal static class TestContexts
{
    public static OpenApiDocumentTransformerContext ForCluster(
        OpenApiDocument document,
        IServiceProvider services,
        string clusterName = "test-cluster",
        YarpOpenApiClusterOptions? options = null,
        params RouteConfig[] routes)
    {
        RouteConfig[] effectiveRoutes = routes.Length == 0
            ? [FakeYarp.Route("route", clusterName, "/api/{**catch-all}")]
            : routes;

        return new OpenApiDocumentTransformerContext
        {
            ClusterName = clusterName,
            Routes = effectiveRoutes,
            Cluster = FakeYarp.Cluster(clusterName, "https://localhost:5101"),
            Options = options ?? new YarpOpenApiClusterOptions(),
            Document = document,
            Services = services,
        };
    }
}
