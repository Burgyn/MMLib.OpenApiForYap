using MMLib.OpenApiForYarp.Abstractions;

namespace MMLib.OpenApiForYarp.Pipeline;

/// <summary>
/// Holds the ordered lists of transformer types that make up the pipeline. Registered as a
/// singleton; mutated by the fluent builder during application startup. Keeping the order here
/// (rather than relying on DI registration order) lets the built-ins be cleared or reordered.
/// </summary>
internal sealed class OpenApiTransformerRegistry
{
    /// <summary>Document transformer types, in execution order (built-ins first by default).</summary>
    public List<Type> DocumentTransformers { get; } = [];

    /// <summary>Operation transformer types, in execution order. Run after all document transformers.</summary>
    public List<Type> OperationTransformers { get; } = [];

    /// <summary>Schema transformer types, in execution order. Run after operation transformers.</summary>
    public List<Type> SchemaTransformers { get; } = [];

    public void AddDocumentTransformer(Type type) => DocumentTransformers.Add(type);

    public void AddOperationTransformer(Type type) => OperationTransformers.Add(type);

    public void AddSchemaTransformer(Type type) => SchemaTransformers.Add(type);

    /// <summary>Removes all document transformers (including built-ins) so a custom order can be defined.</summary>
    public void ClearDocumentTransformers() => DocumentTransformers.Clear();
}
