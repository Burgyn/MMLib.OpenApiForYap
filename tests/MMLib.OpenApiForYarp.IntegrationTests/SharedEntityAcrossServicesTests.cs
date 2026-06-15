using Microsoft.OpenApi;

namespace MMLib.OpenApiForYarp.IntegrationTests;

/// <summary>
/// Two different services expose the SAME entity (a shared <c>Money</c> value object). Each per-service
/// document carries its own <c>Money</c> schema; the merged document de-duplicates them into a single
/// component (keep-first), which is exactly how a shared contract type behaves across microservices.
/// </summary>
public class SharedEntityAcrossServicesTests
{
    private const string ProductsAuthority = "localhost:5301";
    private const string OrdersAuthority = "localhost:5302";

    // Identical Money schema is defined by BOTH downstream services.
    private const string MoneySchema =
        "\"Money\": { \"type\": \"object\", \"required\": [\"amount\",\"currency\"], \"properties\": { \"amount\": { \"type\": \"number\", \"format\": \"double\" }, \"currency\": { \"type\": \"string\" } } }";

    private static string ProductsSpec => $$"""
        {
          "openapi": "3.0.1",
          "info": { "title": "Products", "version": "1.0.0" },
          "paths": {
            "/products/{id}/price": {
              "get": { "operationId": "getProductPrice",
                "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "string" } } ],
                "responses": { "200": { "description": "OK", "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Money" } } } } } }
            }
          },
          "components": { "schemas": { {{MoneySchema}} } }
        }
        """;

    private static string OrdersSpec => $$"""
        {
          "openapi": "3.0.1",
          "info": { "title": "Orders", "version": "1.0.0" },
          "paths": {
            "/orders/{id}/total": {
              "get": { "operationId": "getOrderTotal",
                "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "string" } } ],
                "responses": { "200": { "description": "OK", "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Money" } } } } } }
            }
          },
          "components": { "schemas": { {{MoneySchema}} } }
        }
        """;

    private const string Config = """
        {
          "ReverseProxy": {
            "Routes": {
              "products-route": { "ClusterId": "products-cluster", "Match": { "Path": "/api/products/{**catch-all}" }, "Transforms": [ { "PathPattern": "/products/{**catch-all}" } ] },
              "orders-route": { "ClusterId": "orders-cluster", "Match": { "Path": "/api/orders/{**catch-all}" }, "Transforms": [ { "PathPattern": "/orders/{**catch-all}" } ] }
            },
            "Clusters": {
              "products-cluster": { "Destinations": { "default": { "Address": "http://localhost:5301" } } },
              "orders-cluster": { "Destinations": { "default": { "Address": "http://localhost:5302" } } }
            }
          },
          "YarpOpenApi": {
            "MergeDocuments": true,
            "MergedDocument": { "Title": "Gateway API", "Version": "1.0.0" },
            "Clusters": { "products-cluster": { "Title": "Products" }, "orders-cluster": { "Title": "Orders" } }
          }
        }
        """;

    private static async Task<OpenApiDocument> GetAsync(HttpClient client, string path, CancellationToken ct)
    {
        HttpResponseMessage response = await client.GetAsync(path, ct);
        response.EnsureSuccessStatusCode();
        OpenApiDocument? doc = OpenApiDocument.Parse(await response.Content.ReadAsStringAsync(ct), "json").Document;
        doc.ShouldNotBeNull();
        return doc!;
    }

    [Fact]
    public async Task SharedEntity_AppearsPerService_AndIsDeduplicatedInMergedDocument()
    {
        var handler = new RoutingStubHandler(new Dictionary<string, string>
        {
            [ProductsAuthority] = ProductsSpec,
            [OrdersAuthority] = OrdersSpec,
        });
        await using var host = await GatewayTestHost.StartAsync(Config, handler);
        var ct = CancellationToken.None;

        // The same entity is present in each per-service document.
        OpenApiDocument products = await GetAsync(host.Client, "/openapi/products-cluster.json", ct);
        OpenApiDocument orders = await GetAsync(host.Client, "/openapi/orders-cluster.json", ct);
        products.Components!.Schemas!.ShouldContainKey("Money");
        orders.Components!.Schemas!.ShouldContainKey("Money");

        // In the merged document it is a single, de-duplicated component shared by both services' paths.
        OpenApiDocument merged = await GetAsync(host.Client, "/openapi/all.json", ct);
        merged.Components!.Schemas!.ShouldContainKey("Money");
        merged.Paths.ShouldContainKey("/api/products/{id}/price");
        merged.Paths.ShouldContainKey("/api/orders/{id}/total");

        // The $ref to the shared schema is preserved end-to-end.
        OpenApiOperation price = ((OpenApiPathItem)merged.Paths["/api/products/{id}/price"]).Operations![System.Net.Http.HttpMethod.Get];
        price.Responses!["200"].Content!["application/json"].Schema.ShouldBeOfType<OpenApiSchemaReference>();
    }
}
