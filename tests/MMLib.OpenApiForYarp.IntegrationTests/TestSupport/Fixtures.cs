namespace MMLib.OpenApiForYarp.IntegrationTests;

/// <summary>Inline downstream OpenAPI specs and gateway configuration used by the integration tests.</summary>
internal static class Fixtures
{
    public const string ProductsAuthority = "localhost:5101";
    public const string OrdersAuthority = "localhost:5102";

    public const string ProductsSpec = """
        {
          "openapi": "3.0.1",
          "info": { "title": "Products Service", "version": "1.0.0" },
          "paths": {
            "/products": { "get": { "responses": { "200": { "description": "OK" } } } },
            "/products/{id}": { "get": { "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "string" } } ], "responses": { "200": { "description": "OK" } } } }
          },
          "components": {
            "schemas": { "Product": { "type": "object", "properties": { "id": { "type": "string" } } } },
            "securitySchemes": { "Bearer": { "type": "http", "scheme": "bearer", "bearerFormat": "JWT" } }
          }
        }
        """;

    public const string OrdersSpec = """
        {
          "openapi": "3.0.1",
          "info": { "title": "Orders Service", "version": "1.0.0" },
          "paths": {
            "/orders": { "get": { "responses": { "200": { "description": "OK" } } } },
            "/orders/{id}": { "get": { "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "string" } } ], "responses": { "200": { "description": "OK" } } } },
            "/internal/health": { "get": { "responses": { "200": { "description": "OK" } } } }
          },
          "components": {
            "securitySchemes": { "Bearer": { "type": "http", "scheme": "bearer", "bearerFormat": "JWT" } }
          }
        }
        """;

    /// <summary>Builds a gateway configuration with products + orders clusters.</summary>
    public static string GatewayConfig(bool mergeDocuments = false, bool ordersAddOnlyPublished = false, string productsAddress = "https://localhost:5101", string cacheDuration = "00:01:00")
        => $$"""
        {
          "ReverseProxy": {
            "Routes": {
              "products-route": {
                "ClusterId": "products-cluster",
                "Match": { "Path": "/api/products/{**catch-all}" },
                "Transforms": [ { "PathPattern": "/products/{**catch-all}" } ]
              },
              "orders-route": {
                "ClusterId": "orders-cluster",
                "Match": { "Path": "/api/orders/{**catch-all}" },
                "Transforms": [ { "PathPattern": "/orders/{**catch-all}" } ]
              }
            },
            "Clusters": {
              "products-cluster": { "Destinations": { "default": { "Address": "{{productsAddress}}" } } },
              "orders-cluster": { "Destinations": { "default": { "Address": "https://localhost:5102" } } }
            }
          },
          "YarpOpenApi": {
            "CacheDuration": "{{cacheDuration}}",
            "MergeDocuments": {{(mergeDocuments ? "true" : "false")}},
            "MergedDocument": { "Title": "Gateway API", "Version": "1.0.0" },
            "Clusters": {
              "products-cluster": { "Title": "Products API", "OpenApiPath": "/openapi/v1.json" },
              "orders-cluster": { "Title": "Orders API", "OpenApiPath": "/openapi/v1.json", "AddOnlyPublishedPaths": {{(ordersAddOnlyPublished ? "true" : "false")}} }
            }
          }
        }
        """;

    public static RoutingStubHandler BothServices() => new(new Dictionary<string, string>
    {
        [ProductsAuthority] = ProductsSpec,
        [OrdersAuthority] = OrdersSpec,
    });
}
