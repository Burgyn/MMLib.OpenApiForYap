using Microsoft.OpenApi;

namespace MMLib.OpenApiForYarp.Abstractions;

/// <summary>
/// Transforms a whole aggregated downstream document. Implementations are run in registration
/// order; the built-in path rewrite, security propagation, and published-paths filter are
/// themselves document transformers and can be reordered, removed, or replaced.
/// </summary>
public interface IOpenApiDocumentTransformer
{
    /// <summary>Transforms <paramref name="document"/> in place.</summary>
    Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken);
}

/// <summary>Transforms a single operation. Runs after all document transformers, in registration order.</summary>
public interface IOpenApiOperationTransformer
{
    /// <summary>Transforms <paramref name="operation"/> in place.</summary>
    Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken);
}

/// <summary>Transforms a single component schema. Runs after operation transformers, in registration order.</summary>
public interface IOpenApiSchemaTransformer
{
    /// <summary>Transforms <paramref name="schema"/> in place.</summary>
    Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken);
}
