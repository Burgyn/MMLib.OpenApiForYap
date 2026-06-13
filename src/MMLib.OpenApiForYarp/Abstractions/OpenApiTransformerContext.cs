using System.Net.Http;
using Microsoft.OpenApi;
using MMLib.OpenApiForYarp.Configuration;
using Yarp.ReverseProxy.Configuration;

namespace MMLib.OpenApiForYarp.Abstractions;

/// <summary>
/// Information shared by every transformer: which downstream cluster the document came from, the
/// YARP routes and cluster that target it, the per-cluster options, and the DI container.
/// </summary>
public abstract class OpenApiTransformerContext
{
    /// <summary>The id of the YARP cluster this document was fetched from.</summary>
    public required string ClusterName { get; init; }

    /// <summary>All YARP routes that target <see cref="ClusterName"/>.</summary>
    public required IReadOnlyList<RouteConfig> Routes { get; init; }

    /// <summary>The first route targeting the cluster, or <see langword="null"/> if none.</summary>
    public RouteConfig? Route => Routes.Count > 0 ? Routes[0] : null;

    /// <summary>The YARP cluster configuration (destinations, etc.).</summary>
    public required ClusterConfig Cluster { get; init; }

    /// <summary>The per-cluster aggregation options.</summary>
    public required YarpOpenApiClusterOptions Options { get; init; }

    /// <summary>The document being transformed.</summary>
    public required OpenApiDocument Document { get; init; }

    /// <summary>The request service provider, for resolving additional services.</summary>
    public required IServiceProvider Services { get; init; }
}

/// <summary>Context passed to <see cref="IOpenApiDocumentTransformer"/>.</summary>
public sealed class OpenApiDocumentTransformerContext : OpenApiTransformerContext;

/// <summary>Context passed to <see cref="IOpenApiOperationTransformer"/>.</summary>
public sealed class OpenApiOperationTransformerContext : OpenApiTransformerContext
{
    /// <summary>The path (key in <see cref="OpenApiTransformerContext.Document"/>) the operation belongs to.</summary>
    public required string Path { get; init; }

    /// <summary>The HTTP method of the operation.</summary>
    public required HttpMethod Method { get; init; }
}

/// <summary>Context passed to <see cref="IOpenApiSchemaTransformer"/>.</summary>
public sealed class OpenApiSchemaTransformerContext : OpenApiTransformerContext
{
    /// <summary>The component schema name (key in <c>components.schemas</c>).</summary>
    public required string SchemaName { get; init; }
}
