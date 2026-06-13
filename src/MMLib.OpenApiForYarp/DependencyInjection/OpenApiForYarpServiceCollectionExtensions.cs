using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.ServiceDiscovery;
using MMLib.OpenApiForYarp;
using MMLib.OpenApiForYarp.Aggregation;
using MMLib.OpenApiForYarp.Configuration;
using MMLib.OpenApiForYarp.DependencyInjection;
using MMLib.OpenApiForYarp.Fetching;
using MMLib.OpenApiForYarp.Pipeline;
using MMLib.OpenApiForYarp.Transformers;
using MMLib.OpenApiForYarp.Yarp;
using Yarp.ReverseProxy.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registration entry point for OpenAPI aggregation on a YARP gateway.</summary>
public static class OpenApiForYarpServiceCollectionExtensions
{
    /// <summary>
    /// Adds OpenAPI aggregation to the YARP reverse proxy: binds the <c>YarpOpenApi</c> configuration
    /// section, registers the downstream fetch/cache pipeline, the built-in transformers (path
    /// rewrite, security propagation, published-paths filter), and returns a builder for further
    /// customization.
    /// </summary>
    /// <param name="reverseProxy">The YARP reverse proxy builder.</param>
    /// <param name="configure">Optional code-based configuration applied after binding the config section.</param>
    /// <returns>A builder for registering custom transformers.</returns>
    public static IOpenApiForYarpBuilder AddOpenApiForYarp(this IReverseProxyBuilder reverseProxy, Action<YarpOpenApiOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(reverseProxy);

        IServiceCollection services = reverseProxy.Services;

        OptionsBuilder<YarpOpenApiOptions> optionsBuilder = services
            .AddOptions<YarpOpenApiOptions>()
            .BindConfiguration(YarpOpenApiOptions.SectionName);
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        services.AddMemoryCache();
        services.AddHttpClient(DownstreamOpenApiClient.HttpClientName);

        services.TryAddSingleton<YarpConfigSource>();
        services.TryAddSingleton<IServiceDestinationResolver>(static sp =>
        {
            ServiceEndpointResolver? resolver = sp.GetService<ServiceEndpointResolver>();
            return resolver is not null
                ? new ServiceDiscoveryDestinationResolver(resolver, sp.GetRequiredService<ILogger<ServiceDiscoveryDestinationResolver>>())
                : new StaticDestinationResolver();
        });
        services.TryAddSingleton<IDownstreamOpenApiClient, DownstreamOpenApiClient>();
        services.TryAddSingleton<OpenApiDocumentMerger>();
        services.TryAddScoped<IClusterDocumentBuilder, ClusterDocumentBuilder>();
        services.TryAddSingleton<IClusterDocumentSource, ClusterDocumentSource>();

        var registry = new OpenApiTransformerRegistry();
        services.TryAddSingleton(registry);
        services.TryAddSingleton<OpenApiTransformerPipeline>();

        services.TryAddSingleton<PathRewriteTransformer>();
        services.TryAddSingleton<SecurityPropagationTransformer>();
        services.TryAddSingleton<PublishedPathsFilterTransformer>();

        registry.AddDocumentTransformer(typeof(PathRewriteTransformer));
        registry.AddDocumentTransformer(typeof(SecurityPropagationTransformer));
        registry.AddDocumentTransformer(typeof(PublishedPathsFilterTransformer));

        return new OpenApiForYarpBuilder(services, registry);
    }
}
