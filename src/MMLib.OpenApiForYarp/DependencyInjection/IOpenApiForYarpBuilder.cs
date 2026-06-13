using MMLib.OpenApiForYarp.Abstractions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Fluent builder returned by <see cref="OpenApiForYarpServiceCollectionExtensions.AddOpenApiForYarp"/>
/// for registering transformers and customizing the pipeline.
/// </summary>
public interface IOpenApiForYarpBuilder
{
    /// <summary>The underlying service collection.</summary>
    IServiceCollection Services { get; }

    /// <summary>Appends a document transformer to run after the built-in transformers.</summary>
    IOpenApiForYarpBuilder AddDocumentTransformer<T>()
        where T : class, IOpenApiDocumentTransformer;

    /// <summary>Appends an operation transformer (runs after all document transformers).</summary>
    IOpenApiForYarpBuilder AddOperationTransformer<T>()
        where T : class, IOpenApiOperationTransformer;

    /// <summary>Appends a schema transformer (runs after operation transformers).</summary>
    IOpenApiForYarpBuilder AddSchemaTransformer<T>()
        where T : class, IOpenApiSchemaTransformer;

    /// <summary>
    /// Registers a single class as both a YARP <c>ITransformFactory</c> (the proxy transform) and,
    /// if it implements <see cref="IOpenApiDocumentTransformer"/>, an OpenAPI document transformer —
    /// wiring both from one registration. The class must implement at least one of the two.
    /// </summary>
    IOpenApiForYarpBuilder AddTransformFactory<T>()
        where T : class;

    /// <summary>
    /// Removes all document transformers (including the built-in path rewrite, security propagation,
    /// and published-paths filter) so a completely custom order can be registered.
    /// </summary>
    IOpenApiForYarpBuilder ClearOpenApiTransformers();
}
