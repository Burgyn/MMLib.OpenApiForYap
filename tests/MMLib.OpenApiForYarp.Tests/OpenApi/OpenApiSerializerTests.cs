using MMLib.OpenApiForYarp.OpenApi;

namespace MMLib.OpenApiForYarp.Tests.OpenApi;

public class OpenApiSerializerTests
{
    private const string SampleJson = """
        {
          "openapi": "3.0.1",
          "info": { "title": "Products API", "version": "1.0.0" },
          "paths": {
            "/products/{id}": {
              "get": {
                "operationId": "getProduct",
                "parameters": [
                  { "name": "id", "in": "path", "required": true, "schema": { "type": "string" } }
                ],
                "responses": { "200": { "description": "OK" } }
              }
            }
          }
        }
        """;

    [Fact]
    public void Parse_ReadsInfoAndPaths()
    {
        (var document, var diagnostic) = OpenApiSerializer.Parse(SampleJson);

        document.ShouldNotBeNull();
        diagnostic!.Errors.ShouldBeEmpty();
        document!.Info!.Title.ShouldBe("Products API");
        document.Paths.ShouldContainKey("/products/{id}");
    }

    [Fact]
    public async Task RoundTrip_PreservesPathsAndInfo()
    {
        (var document, _) = OpenApiSerializer.Parse(SampleJson);

        string json = await OpenApiSerializer.SerializeAsync(document!);
        (var reparsed, _) = OpenApiSerializer.Parse(json);

        reparsed.ShouldNotBeNull();
        reparsed!.Info!.Title.ShouldBe("Products API");
        reparsed.Paths.ShouldContainKey("/products/{id}");
    }

    [Fact]
    public void ResolveSpecVersion_ReadsFromDiagnostic()
    {
        (_, var diagnostic) = OpenApiSerializer.Parse(SampleJson);

        OpenApiSerializer.ResolveSpecVersion(diagnostic).ShouldBe(Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0);
    }
}
