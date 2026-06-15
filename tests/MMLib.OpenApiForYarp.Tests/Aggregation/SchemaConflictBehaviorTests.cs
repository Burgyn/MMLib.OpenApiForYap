using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using MMLib.OpenApiForYarp.Aggregation;
using MMLib.OpenApiForYarp.Configuration;
using MMLib.OpenApiForYarp.OpenApi;

namespace MMLib.OpenApiForYarp.Tests.Aggregation;

/// <summary>
/// How the merger resolves component-schema name collisions across services: identical shapes merge
/// silently; different shapes keep-first + warn by default, or (with RenameDuplicateSchemas) the
/// colliding schema is renamed and its service's refs are rewritten so nothing is lost.
/// </summary>
public class SchemaConflictBehaviorTests
{
    private static OpenApiDocument Parse(string json) => OpenApiSerializer.Parse(json).Document!;

    private const string ProductsWithMoneyAmount = """
        {
          "openapi": "3.0.1", "info": { "title": "Products", "version": "1.0.0" },
          "paths": { "/api/products/{id}/price": { "get": { "responses": { "200": { "description": "OK",
            "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Money" } } } } } } } },
          "components": { "schemas": { "Money": { "type": "object", "properties": {
            "amount": { "type": "number", "format": "double" }, "currency": { "type": "string" } } } } }
        }
        """;

    // Same name "Money", DIFFERENT shape (value/iso/symbol).
    private const string OrdersWithMoneyValue = """
        {
          "openapi": "3.0.1", "info": { "title": "Orders", "version": "1.0.0" },
          "paths": { "/api/orders/{id}/total": { "get": { "responses": { "200": { "description": "OK",
            "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Money" } } } } } } } },
          "components": { "schemas": { "Money": { "type": "object", "properties": {
            "value": { "type": "number" }, "iso": { "type": "string" }, "symbol": { "type": "string" } } } } }
        }
        """;

    // Different path, IDENTICAL Money shape (amount/currency) — a genuinely shared contract.
    private const string OrdersWithMoneyAmount = """
        {
          "openapi": "3.0.1", "info": { "title": "Orders", "version": "1.0.0" },
          "paths": { "/api/orders/{id}/total": { "get": { "responses": { "200": { "description": "OK",
            "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Money" } } } } } } } },
          "components": { "schemas": { "Money": { "type": "object", "properties": {
            "amount": { "type": "number", "format": "double" }, "currency": { "type": "string" } } } } }
        }
        """;

    private static OpenApiDocument Merge(RecordingLogger<OpenApiDocumentMerger> logger, bool rename, params (string ClusterId, OpenApiDocument Document)[] docs)
        => new OpenApiDocumentMerger(logger).Merge(docs, new MergedDocumentOptions { Title = "Gateway", Version = "1.0.0", RenameDuplicateSchemas = rename });

    [Fact]
    public void DifferentShapes_Default_KeepsFirst_DropsSecond_AndWarns()
    {
        var logger = new RecordingLogger<OpenApiDocumentMerger>();
        OpenApiDocument merged = Merge(logger, rename: false,
            ("products-cluster", Parse(ProductsWithMoneyAmount)),
            ("orders-cluster", Parse(OrdersWithMoneyValue)));

        // One "Money" survives — the first service's shape; the second is dropped.
        var money = (OpenApiSchema)merged.Components!.Schemas!["Money"];
        money.Properties!.Keys.ShouldBe(["amount", "currency"], ignoreOrder: true);
        merged.Components.Schemas.Keys.ShouldNotContain(k => k.Contains("OrdersCluster"));
        logger.Entries.ShouldContain(e => e.Level == LogLevel.Warning && e.Message.Contains("Money"));
    }

    [Fact]
    public void DifferentShapes_WithRename_KeepsBoth_AndRewritesRefs()
    {
        var logger = new RecordingLogger<OpenApiDocumentMerger>();
        OpenApiDocument merged = Merge(logger, rename: true,
            ("products-cluster", Parse(ProductsWithMoneyAmount)),
            ("orders-cluster", Parse(OrdersWithMoneyValue)));

        // Both shapes are preserved under distinct names — nothing is lost.
        merged.Components!.Schemas!.ShouldContainKey("Money");
        merged.Components!.Schemas!.ShouldContainKey("OrdersClusterMoney");
        ((OpenApiSchema)merged.Components!.Schemas!["Money"]).Properties!.Keys.ShouldBe(["amount", "currency"], ignoreOrder: true);
        ((OpenApiSchema)merged.Components!.Schemas!["OrdersClusterMoney"]).Properties!.Keys.ShouldBe(["value", "iso", "symbol"], ignoreOrder: true);

        // Each service's operation references the right schema in the merged document.
        JsonNode root = JsonNode.Parse(OpenApiSerializer.SerializeToJson(merged))!;
        Ref(root, "/api/products/{id}/price").ShouldBe("#/components/schemas/Money");
        Ref(root, "/api/orders/{id}/total").ShouldBe("#/components/schemas/OrdersClusterMoney");

        // A rename is informational, not a warning.
        logger.Entries.ShouldNotContain(e => e.Level == LogLevel.Warning);

        static string? Ref(JsonNode root, string path) =>
            root["paths"]![path]!["get"]!["responses"]!["200"]!["content"]!["application/json"]!["schema"]!["$ref"]!.GetValue<string>();
    }

    [Fact]
    public void IdenticalShapes_MergeSilently_WithoutWarning()
    {
        var logger = new RecordingLogger<OpenApiDocumentMerger>();
        OpenApiDocument merged = Merge(logger, rename: false,
            ("products-cluster", Parse(ProductsWithMoneyAmount)),
            ("orders-cluster", Parse(OrdersWithMoneyAmount)));

        merged.Components!.Schemas!.ShouldContainKey("Money");
        merged.Components!.Schemas!.Count.ShouldBe(1);
        logger.HasWarning.ShouldBeFalse();
    }
}
