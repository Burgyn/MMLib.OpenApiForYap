using System.Globalization;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace MMLib.OpenApiForYarp.OpenApi;

/// <summary>
/// Thin isolation layer over the <c>Microsoft.OpenApi</c> reader/writer API. Keeping every
/// parse/serialize call behind this type localizes any future <c>Microsoft.OpenApi</c> version
/// change to a single file.
/// </summary>
internal static class OpenApiSerializer
{
    /// <summary>Parses an OpenAPI JSON document into the object model.</summary>
    /// <param name="json">The OpenAPI document as JSON.</param>
    /// <returns>The parsed document (or <see langword="null"/> on a fatal parse error) and the diagnostic.</returns>
    public static (OpenApiDocument? Document, OpenApiDiagnostic? Diagnostic) Parse(string json)
    {
        ReadResult result = OpenApiDocument.Parse(json, format: "json");
        return (result.Document, result.Diagnostic);
    }

    /// <summary>Serializes a document to JSON in the requested OpenAPI version.</summary>
    public static Task<string> SerializeAsync(
        OpenApiDocument document,
        OpenApiSpecVersion version = OpenApiSpecVersion.OpenApi3_0,
        CancellationToken cancellationToken = default)
        => document.SerializeAsJsonAsync(version, cancellationToken);

    /// <summary>
    /// Maps a parsed document's diagnostic to the spec version it was read as, so transformed
    /// output can preserve the original OpenAPI version where desired.
    /// </summary>
    public static OpenApiSpecVersion ResolveSpecVersion(OpenApiDiagnostic? diagnostic, OpenApiSpecVersion fallback = OpenApiSpecVersion.OpenApi3_0)
        => diagnostic?.SpecificationVersion ?? fallback;

    /// <summary>Synchronously serializes any OpenAPI element to an OpenAPI 3.0 JSON string.</summary>
    public static string SerializeToJson(IOpenApiSerializable element)
    {
        using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
        var writer = new OpenApiJsonWriter(stringWriter);
        element.SerializeAsV3(writer);
        stringWriter.Flush();
        return stringWriter.ToString();
    }
}
