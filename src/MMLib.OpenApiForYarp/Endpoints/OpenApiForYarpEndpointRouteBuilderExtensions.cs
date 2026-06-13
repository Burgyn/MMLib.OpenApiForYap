using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using MMLib.OpenApiForYarp.Aggregation;
using MMLib.OpenApiForYarp.Configuration;
using MMLib.OpenApiForYarp.OpenApi;

namespace Microsoft.AspNetCore.Builder;

/// <summary>Maps the per-cluster and merged aggregated OpenAPI document endpoints.</summary>
public static class OpenApiForYarpEndpointRouteBuilderExtensions
{
    private const string JsonContentType = "application/json; charset=utf-8";

    /// <summary>
    /// Maps <c>GET /openapi/{cluster}.json</c> (one transformed document per downstream cluster) and,
    /// when <see cref="YarpOpenApiOptions.MergeDocuments"/> is enabled, the merged document route.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>A builder for the mapped endpoints.</returns>
    public static IEndpointConventionBuilder MapOpenApiForYarp(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        YarpOpenApiOptions options = endpoints.ServiceProvider.GetRequiredService<IOptions<YarpOpenApiOptions>>().Value;
        RouteGroupBuilder group = endpoints.MapGroup(string.Empty);

        group.MapGet(options.DocumentRoutePattern, async (
            string cluster,
            IClusterDocumentBuilder builder,
            CancellationToken cancellationToken) =>
        {
            OpenApiDocument? document = await builder.BuildAsync(cluster, cancellationToken).ConfigureAwait(false);
            if (document is null)
            {
                return Results.NotFound();
            }

            string json = await OpenApiSerializer.SerializeAsync(document, OpenApiSpecVersion.OpenApi3_0, cancellationToken).ConfigureAwait(false);
            return Results.Text(json, JsonContentType);
        });

        if (options.MergeDocuments)
        {
            group.MapGet(options.MergedDocument.RoutePattern, async (
                IClusterDocumentBuilder builder,
                OpenApiDocumentMerger merger,
                CancellationToken cancellationToken) =>
            {
                List<(string ClusterId, OpenApiDocument Document)> documents = [];
                foreach (string clusterId in builder.GetClusterIds())
                {
                    OpenApiDocument? document = await builder.BuildAsync(clusterId, cancellationToken).ConfigureAwait(false);
                    if (document is not null)
                    {
                        documents.Add((clusterId, document));
                    }
                }

                OpenApiDocument merged = merger.Merge(documents, options.MergedDocument);
                string json = await OpenApiSerializer.SerializeAsync(merged, OpenApiSpecVersion.OpenApi3_0, cancellationToken).ConfigureAwait(false);
                return Results.Text(json, JsonContentType);
            });
        }

        return group;
    }
}
