using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using MMLib.OpenApiForYarp.Abstractions;

namespace MMLib.OpenApiForYarp.Pipeline;

/// <summary>
/// Runs the transformer pipeline over a single cluster document: every document transformer in
/// order, then every operation transformer across all operations, then every schema transformer
/// across all component schemas.
/// </summary>
internal sealed class OpenApiTransformerPipeline(OpenApiTransformerRegistry registry)
{
    private readonly OpenApiTransformerRegistry _registry = registry;

    public async Task RunAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        IServiceProvider services = context.Services;

        foreach (Type type in _registry.DocumentTransformers)
        {
            var transformer = (IOpenApiDocumentTransformer)services.GetRequiredService(type);
            await transformer.TransformAsync(document, context, cancellationToken).ConfigureAwait(false);
        }

        await RunOperationTransformersAsync(document, context, services, cancellationToken).ConfigureAwait(false);
        await RunSchemaTransformersAsync(document, context, services, cancellationToken).ConfigureAwait(false);
    }

    private async Task RunOperationTransformersAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        if (_registry.OperationTransformers.Count == 0 || document.Paths is null)
        {
            return;
        }

        var transformers = _registry.OperationTransformers
            .Select(type => (IOpenApiOperationTransformer)services.GetRequiredService(type))
            .ToArray();

        foreach ((string path, IOpenApiPathItem pathItem) in document.Paths)
        {
            if (pathItem is not OpenApiPathItem { Operations: { } operations })
            {
                continue;
            }

            foreach ((System.Net.Http.HttpMethod method, OpenApiOperation operation) in operations)
            {
                var operationContext = new OpenApiOperationTransformerContext
                {
                    ClusterName = context.ClusterName,
                    Routes = context.Routes,
                    Cluster = context.Cluster,
                    Options = context.Options,
                    Document = document,
                    Services = services,
                    Path = path,
                    Method = method,
                };

                foreach (IOpenApiOperationTransformer transformer in transformers)
                {
                    await transformer.TransformAsync(operation, operationContext, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    private async Task RunSchemaTransformersAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        if (_registry.SchemaTransformers.Count == 0 || document.Components?.Schemas is not { } schemas)
        {
            return;
        }

        var transformers = _registry.SchemaTransformers
            .Select(type => (IOpenApiSchemaTransformer)services.GetRequiredService(type))
            .ToArray();

        foreach ((string name, IOpenApiSchema schema) in schemas)
        {
            if (schema is not OpenApiSchema concreteSchema)
            {
                continue;
            }

            var schemaContext = new OpenApiSchemaTransformerContext
            {
                ClusterName = context.ClusterName,
                Routes = context.Routes,
                Cluster = context.Cluster,
                Options = context.Options,
                Document = document,
                Services = services,
                SchemaName = name,
            };

            foreach (IOpenApiSchemaTransformer transformer in transformers)
            {
                await transformer.TransformAsync(concreteSchema, schemaContext, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
