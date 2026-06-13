using Microsoft.Extensions.DependencyInjection.Extensions;
using MMLib.OpenApiForYarp.Abstractions;
using MMLib.OpenApiForYarp.Pipeline;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Microsoft.Extensions.DependencyInjection;

/// <inheritdoc cref="IOpenApiForYarpBuilder"/>
internal sealed class OpenApiForYarpBuilder(IServiceCollection services, OpenApiTransformerRegistry registry) : IOpenApiForYarpBuilder
{
    private readonly OpenApiTransformerRegistry _registry = registry;

    public IServiceCollection Services { get; } = services;

    public IOpenApiForYarpBuilder AddDocumentTransformer<T>()
        where T : class, IOpenApiDocumentTransformer
    {
        Services.TryAddTransient<T>();
        _registry.AddDocumentTransformer(typeof(T));
        return this;
    }

    public IOpenApiForYarpBuilder AddOperationTransformer<T>()
        where T : class, IOpenApiOperationTransformer
    {
        Services.TryAddTransient<T>();
        _registry.AddOperationTransformer(typeof(T));
        return this;
    }

    public IOpenApiForYarpBuilder AddSchemaTransformer<T>()
        where T : class, IOpenApiSchemaTransformer
    {
        Services.TryAddTransient<T>();
        _registry.AddSchemaTransformer(typeof(T));
        return this;
    }

    public IOpenApiForYarpBuilder AddTransformFactory<T>()
        where T : class
    {
        Services.TryAddSingleton<T>();

        bool wired = false;

        if (typeof(ITransformFactory).IsAssignableFrom(typeof(T)))
        {
            // Forward YARP's proxy transform factory to the same singleton instance.
            Services.AddSingleton<ITransformFactory>(sp => (ITransformFactory)sp.GetRequiredService<T>());
            wired = true;
        }

        if (typeof(IOpenApiDocumentTransformer).IsAssignableFrom(typeof(T)))
        {
            _registry.AddDocumentTransformer(typeof(T));
            wired = true;
        }

        if (!wired)
        {
            throw new InvalidOperationException(
                $"'{typeof(T)}' must implement {nameof(ITransformFactory)} and/or {nameof(IOpenApiDocumentTransformer)} to be registered via AddTransformFactory.");
        }

        return this;
    }

    public IOpenApiForYarpBuilder ClearOpenApiTransformers()
    {
        _registry.ClearDocumentTransformers();
        return this;
    }
}
