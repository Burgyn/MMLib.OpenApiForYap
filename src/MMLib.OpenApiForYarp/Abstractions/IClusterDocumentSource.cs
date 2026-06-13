namespace MMLib.OpenApiForYarp;

/// <summary>
/// Describes a document that the gateway exposes, for consumption by UI adapters (Scalar, Swagger UI).
/// </summary>
/// <param name="Name">The document identifier (cluster id, or the merged document name).</param>
/// <param name="Title">The human-readable title shown in the UI.</param>
/// <param name="RoutePattern">The gateway route that serves this document's JSON (e.g. <c>/openapi/products-cluster.json</c>).</param>
public sealed record ClusterDocumentInfo(string Name, string Title, string RoutePattern);

/// <summary>
/// Enumerates the OpenAPI documents the gateway publishes. UI adapters use this to register one
/// entry per downstream cluster (plus the merged document when enabled) without depending on
/// internal types.
/// </summary>
public interface IClusterDocumentSource
{
    /// <summary>The per-cluster documents.</summary>
    IReadOnlyList<ClusterDocumentInfo> GetDocuments();

    /// <summary>Whether the merged document is enabled.</summary>
    bool MergeEnabled { get; }

    /// <summary>The merged document, when <see cref="MergeEnabled"/> is <see langword="true"/>; otherwise <see langword="null"/>.</summary>
    ClusterDocumentInfo? MergedDocument { get; }
}
