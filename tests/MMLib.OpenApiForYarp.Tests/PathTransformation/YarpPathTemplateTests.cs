using MMLib.OpenApiForYarp.PathTransformation;

namespace MMLib.OpenApiForYarp.Tests.PathTransformation;

public class YarpPathTemplateTests
{
    [Theory]
    [InlineData("/api/products/{**catch-all}", "/api/products")]
    [InlineData("{**catch-all}", "")]
    [InlineData("/{**catch-all}", "")]
    [InlineData("/products/{id}", "/products")]
    [InlineData("/products", "/products")]
    [InlineData("/", "")]
    [InlineData("/app/v1/{**catch-all}", "/app/v1")]
    [InlineData("/orders/{orderId}/items/{itemId}", "/orders")]
    public void StaticPrefix_ExtractsLiteralLeadingSegments(string template, string expected)
        => YarpPathTemplate.StaticPrefix(template).ShouldBe(expected);

    [Theory]
    [InlineData("/api/products", "/{id}", "/api/products/{id}")]
    [InlineData("/api/products", "", "/api/products")]
    [InlineData("", "/products/{id}", "/products/{id}")]
    [InlineData("/api", "/products/{id}", "/api/products/{id}")]
    [InlineData("", "", "/")]
    public void Combine_JoinsWithSingleSlash(string prefix, string remainder, string expected)
        => YarpPathTemplate.Combine(prefix, remainder).ShouldBe(expected);
}
