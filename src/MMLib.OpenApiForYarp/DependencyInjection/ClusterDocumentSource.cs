using Microsoft.Extensions.Options;
using MMLib.OpenApiForYarp.Configuration;
using MMLib.OpenApiForYarp.Yarp;

namespace MMLib.OpenApiForYarp.DependencyInjection;

/// <summary>
/// Default <see cref="IClusterDocumentSource"/> backed by the live YARP configuration and the
/// per-cluster options. Computes one document per configured cluster plus the merged document.
/// </summary>
internal sealed class ClusterDocumentSource(YarpConfigSource configSource, IOptions<YarpOpenApiOptions> options) : IClusterDocumentSource
{
    private readonly YarpConfigSource _configSource = configSource;
    private readonly IOptions<YarpOpenApiOptions> _options = options;

    public bool MergeEnabled => _options.Value.MergeDocuments;

    public ClusterDocumentInfo? MergedDocument
    {
        get
        {
            if (!MergeEnabled)
            {
                return null;
            }

            MergedDocumentOptions merged = _options.Value.MergedDocument;
            return new ClusterDocumentInfo(merged.DocumentName, merged.Title, merged.RoutePattern);
        }
    }

    public IReadOnlyList<ClusterDocumentInfo> GetDocuments()
    {
        YarpOpenApiOptions opts = _options.Value;

        return
        [
            .. _configSource.GetClusterIds().Select(id =>
            {
                string title = opts.Clusters.TryGetValue(id, out YarpOpenApiClusterOptions? c) && !string.IsNullOrEmpty(c.Title)
                    ? c.Title!
                    : id;
                return new ClusterDocumentInfo(id, title, opts.GetDocumentRoute(id));
            }),
        ];
    }
}
