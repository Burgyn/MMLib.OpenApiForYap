namespace MMLib.OpenApiForYarp.Configuration;

/// <summary>
/// Describes the <c>info</c> block of the merged document served at <c>/openapi/all.json</c>.
/// These values come from the gateway's own configuration, not from any downstream service.
/// </summary>
public sealed class MergedDocumentOptions
{
    /// <summary>The title of the merged document. Defaults to <c>"Gateway API"</c>.</summary>
    public string Title { get; set; } = "Gateway API";

    /// <summary>The version of the merged document. Defaults to <c>"1.0.0"</c>.</summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>An optional description for the merged document.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// The route at which the merged document is served. Defaults to <c>/openapi/all.json</c>.
    /// </summary>
    public string RoutePattern { get; set; } = "/openapi/all.json";

    /// <summary>The identifier used for the merged document in UI adapters. Defaults to <c>"all"</c>.</summary>
    public string DocumentName { get; set; } = "all";

    /// <summary>
    /// Controls how component-schema name collisions between services are resolved in the merged
    /// document. When <see langword="true"/>, a schema whose name already exists with <em>different</em>
    /// content is renamed (prefixed with the owning cluster) and that service's <c>$ref</c>s are
    /// rewritten, so the merged document stays correct for every service. When <see langword="false"/>
    /// (the default) the first occurrence is kept and a warning is logged. Identically-shaped schemas
    /// are always merged silently regardless of this setting.
    /// </summary>
    public bool RenameDuplicateSchemas { get; set; }
}
