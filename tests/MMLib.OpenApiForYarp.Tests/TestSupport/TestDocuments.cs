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
}
