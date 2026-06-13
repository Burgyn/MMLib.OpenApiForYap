using MMLib.OpenApiForYarp.Yarp;

namespace MMLib.OpenApiForYarp.Tests.Yarp;

public class YarpConfigSourceTests
{
    [Fact]
    public void TryGetCluster_ReturnsClusterAndItsRoutes()
    {
        var provider = FakeYarp.Provider(
            routes:
            [
                FakeYarp.Route("products", "products-cluster", "/api/products/{**catch-all}"),
                FakeYarp.Route("orders", "orders-cluster", "/api/orders/{**catch-all}"),
            ],
            clusters:
            [
                FakeYarp.Cluster("products-cluster", "https://localhost:5101"),
                FakeYarp.Cluster("orders-cluster", "https://localhost:5102"),
            ]);
        var source = new YarpConfigSource([provider]);

        source.TryGetCluster("products-cluster", out var descriptor).ShouldBeTrue();

        descriptor.Routes.Count.ShouldBe(1);
        descriptor.Routes[0].RouteId.ShouldBe("products");
        descriptor.Cluster.Destinations!["default"].Address.ShouldBe("https://localhost:5101");
    }

    [Fact]
    public void TryGetCluster_ReturnsAllRoutesForCluster()
    {
        var provider = FakeYarp.Provider(
            routes:
            [
                FakeYarp.Route("r1", "shared", "/api/a/{**catch-all}"),
                FakeYarp.Route("r2", "shared", "/api/b/{**catch-all}"),
                FakeYarp.Route("r3", "shared", "/api/c/{**catch-all}"),
            ],
            clusters: [FakeYarp.Cluster("shared", "https://localhost:5101")]);
        var source = new YarpConfigSource([provider]);

        source.TryGetCluster("shared", out var descriptor).ShouldBeTrue();

        descriptor.Routes.Count.ShouldBe(3);
    }

    [Fact]
    public void TryGetCluster_IsCaseInsensitive()
    {
        var provider = FakeYarp.Provider(
            routes: [FakeYarp.Route("r", "Products-Cluster", "/api/{**catch-all}")],
            clusters: [FakeYarp.Cluster("Products-Cluster", "https://localhost:5101")]);
        var source = new YarpConfigSource([provider]);

        source.TryGetCluster("products-cluster", out _).ShouldBeTrue();
    }

    [Fact]
    public void TryGetCluster_ReturnsFalse_ForUnknownCluster()
    {
        var provider = FakeYarp.Provider(routes: [], clusters: []);
        var source = new YarpConfigSource([provider]);

        source.TryGetCluster("nope", out _).ShouldBeFalse();
    }

    [Fact]
    public void GetClusterIds_ReturnsAllClusters()
    {
        var provider = FakeYarp.Provider(
            routes: [],
            clusters:
            [
                FakeYarp.Cluster("a", "https://localhost:1"),
                FakeYarp.Cluster("b", "https://localhost:2"),
            ]);
        var source = new YarpConfigSource([provider]);

        source.GetClusterIds().ShouldBe(["a", "b"], ignoreOrder: true);
    }
}
