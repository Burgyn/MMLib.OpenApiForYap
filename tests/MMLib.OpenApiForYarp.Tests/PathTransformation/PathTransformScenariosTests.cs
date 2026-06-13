using MMLib.OpenApiForYarp.PathTransformation;
using Yarp.ReverseProxy.Configuration;

namespace MMLib.OpenApiForYarp.Tests.PathTransformation;

/// <summary>
/// Each test is a complete real-world rewrite scenario. The rewriter must compute the
/// gateway-facing path for each downstream path correctly under various YARP route configs.
/// </summary>
public class PathTransformScenariosTests
{
    private readonly YarpPathRewriter _rewriter = new();

    private static RouteConfig Route(string matchPath, params (string Key, string Value)[] transforms)
        => new()
        {
            RouteId = "route",
            ClusterId = "cluster",
            Match = new RouteMatch { Path = matchPath },
            Transforms = transforms.Length == 0
                ? null
                : [.. transforms.Select(t => new Dictionary<string, string> { [t.Key] = t.Value })],
        };

    [Fact]
    public void SimplePrefix_SingleRoute()
    {
        var route = Route("/api/products/{**catch-all}", ("PathPattern", "/products/{**catch-all}"));

        var map = _rewriter.BuildPathMap(route, ["/products/{id}", "/products"]);

        map["/products/{id}"].ShouldBe("/api/products/{id}");
        map["/products"].ShouldBe("/api/products");
    }

    [Fact]
    public void CatchAll_SingleRoute_PreservesNestedPaths()
    {
        var route = Route("/api/orders/{**catch-all}", ("PathPattern", "/orders/{**catch-all}"));

        var map = _rewriter.BuildPathMap(route, ["/orders/{orderId}/items/{itemId}"]);

        map["/orders/{orderId}/items/{itemId}"].ShouldBe("/api/orders/{orderId}/items/{itemId}");
    }

    [Fact]
    public void PathRemovePrefix_Transform()
    {
        // RemovePrefix /api strips /api before forwarding, so the gateway path re-adds it.
        var route = Route("/api/{**catch-all}", ("PathRemovePrefix", "/api"));

        var map = _rewriter.BuildPathMap(route, ["/products/{id}", "/orders"]);

        map["/products/{id}"].ShouldBe("/api/products/{id}");
        map["/orders"].ShouldBe("/api/orders");
    }

    [Fact]
    public void PathPrefix_Transform_StripsAddedPrefix()
    {
        // PathPrefix /internal is added when forwarding, so the gateway path drops it.
        var route = Route("/products/{**catch-all}", ("PathPrefix", "/internal"));

        var map = _rewriter.BuildPathMap(route, ["/internal/products/{id}"]);

        map["/internal/products/{id}"].ShouldBe("/products/{id}");
    }

    [Fact]
    public void PathPattern_WithParameters_AreUntouched()
    {
        var route = Route("/api/products/{**catch-all}", ("PathPattern", "/products/{**catch-all}"));

        var gateway = _rewriter.MapPath(route, "/products/{productId}/reviews/{reviewId}");

        gateway.ShouldBe("/api/products/{productId}/reviews/{reviewId}");
    }

    [Fact]
    public void NoTransform_Passthrough_PathsUnchanged()
    {
        var route = Route("/products/{**catch-all}");

        var map = _rewriter.BuildPathMap(route, ["/products/{id}", "/products"]);

        map["/products/{id}"].ShouldBe("/products/{id}");
        map["/products"].ShouldBe("/products");
    }

    [Fact]
    public void VirtualDirectory_NestedPath()
    {
        // Downstream is hosted under a virtual directory; the catch-all rebases onto the gateway prefix.
        var route = Route("/shop/{**catch-all}", ("PathPattern", "/app/v1/{**catch-all}"));

        var map = _rewriter.BuildPathMap(route, ["/app/v1/products/{id}"]);

        map["/app/v1/products/{id}"].ShouldBe("/shop/products/{id}");
    }

    [Fact]
    public void MultipleTransforms_SameRoute_NonPathTransformsIgnored()
    {
        var route = Route(
            "/api/products/{**catch-all}",
            ("PathPattern", "/products/{**catch-all}"),
            ("RequestHeader", "X-Forwarded"));

        var map = _rewriter.BuildPathMap(route, ["/products/{id}"]);

        map["/products/{id}"].ShouldBe("/api/products/{id}");
    }

    [Fact]
    public void MultipleRoutes_SameCluster_EachPathRebasedByItsRoute()
    {
        var productsRoute = Route("/api/products/{**catch-all}", ("PathPattern", "/products/{**catch-all}"));
        var ordersRoute = Route("/api/orders/{**catch-all}", ("PathPattern", "/orders/{**catch-all}"));

        var document = TestDocuments.WithPaths("/products/{id}", "/orders/{id}", "/orders");
        _rewriter.RewriteDocumentPaths(document, [productsRoute, ordersRoute]);

        document.Paths.ShouldContainKey("/api/products/{id}");
        document.Paths.ShouldContainKey("/api/orders/{id}");
        document.Paths.ShouldContainKey("/api/orders");
        document.Paths.ShouldNotContainKey("/products/{id}");
    }

    [Fact]
    public void RewriteDocumentPaths_PreservesOperations()
    {
        var route = Route("/api/products/{**catch-all}", ("PathPattern", "/products/{**catch-all}"));
        var document = TestDocuments.WithPaths("/products/{id}");

        _rewriter.RewriteDocumentPaths(document, [route]);

        document.Paths["/api/products/{id}"].Operations!.ShouldContainKey(System.Net.Http.HttpMethod.Get);
    }
}
