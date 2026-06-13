using System.Net.Http;
using Microsoft.OpenApi;

namespace MMLib.OpenApiForYarp.Tests;

/// <summary>Builders for in-memory <see cref="OpenApiDocument"/> instances used across tests.</summary>
internal static class TestDocuments
{
    /// <summary>Creates a document with a single GET operation at each of the supplied paths.</summary>
    public static OpenApiDocument WithPaths(params string[] paths)
    {
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test API", Version = "1.0.0" },
            Paths = [],
        };

        foreach (string path in paths)
        {
            document.Paths[path] = new OpenApiPathItem
            {
                Operations = new Dictionary<HttpMethod, OpenApiOperation>
                {
                    [HttpMethod.Get] = new OpenApiOperation
                    {
                        OperationId = "op" + path.Replace("/", "_", StringComparison.Ordinal),
                        Responses = new OpenApiResponses
                        {
                            ["200"] = new OpenApiResponse { Description = "OK" },
                        },
                    },
                },
            };
        }

        return document;
    }

    /// <summary>Adds HTTP bearer security schemes with the given names to the document's components.</summary>
    public static OpenApiDocument WithSecuritySchemes(this OpenApiDocument document, params string[] names)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        foreach (string name in names)
        {
            document.Components.SecuritySchemes[name] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
            };
        }

        return document;
    }

    /// <summary>Adds simple object component schemas with the given names to the document.</summary>
    public static OpenApiDocument WithSchemas(this OpenApiDocument document, params string[] names)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();

        foreach (string name in names)
        {
            document.Components.Schemas[name] = new OpenApiSchema { Type = JsonSchemaType.Object };
        }

        return document;
    }
}
