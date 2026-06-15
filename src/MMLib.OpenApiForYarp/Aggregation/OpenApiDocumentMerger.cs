using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using MMLib.OpenApiForYarp.Configuration;
using MMLib.OpenApiForYarp.OpenApi;

namespace MMLib.OpenApiForYarp.Aggregation;

/// <summary>Merges several transformed cluster documents into one combined document.</summary>
internal sealed class OpenApiDocumentMerger(ILogger<OpenApiDocumentMerger> logger)
{
    private const string SchemaRefPrefix = "#/components/schemas/";

    private readonly ILogger<OpenApiDocumentMerger> _logger = logger;

    /// <summary>
    /// Merges the supplied documents. The merged <c>info</c> comes from <paramref name="info"/>;
    /// paths, component schemas, and security schemes are unioned by key. Identically-shaped
    /// components merge silently; a same-named component with different content keeps the first
    /// occurrence and warns — unless <see cref="MergedDocumentOptions.RenameDuplicateSchemas"/> is
    /// set, in which case the colliding schema is renamed (and its service's refs rewritten).
    /// </summary>
    public OpenApiDocument Merge(IReadOnlyList<(string ClusterId, OpenApiDocument Document)> documents, MergedDocumentOptions info)
    {
        var merged = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = info.Title,
                Version = info.Version,
                Description = info.Description,
            },
            Paths = [],
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal),
                SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>(StringComparer.Ordinal),
            },
        };

        foreach ((string clusterId, OpenApiDocument document) in documents)
        {
            OpenApiDocument prepared = info.RenameDuplicateSchemas
                ? RenameConflictingSchemas(document, merged, clusterId)
                : document;

            MergePaths(merged, prepared, clusterId);
            MergeSchemas(merged, prepared, clusterId);
            MergeSecuritySchemes(merged, prepared, clusterId);
        }

        return merged;
    }

    private void MergePaths(OpenApiDocument merged, OpenApiDocument source, string clusterId)
    {
        if (source.Paths is null)
        {
            return;
        }

        foreach ((string path, IOpenApiPathItem item) in source.Paths)
        {
            if (merged.Paths.ContainsKey(path))
            {
                _logger.LogWarning("Path '{Path}' from cluster '{ClusterId}' conflicts with an existing path; keeping the first occurrence.", path, clusterId);
                continue;
            }

            merged.Paths[path] = item;
        }
    }

    private void MergeSchemas(OpenApiDocument merged, OpenApiDocument source, string clusterId)
    {
        if (source.Components?.Schemas is not { } schemas)
        {
            return;
        }

        foreach ((string name, IOpenApiSchema schema) in schemas)
        {
            if (merged.Components!.Schemas!.TryGetValue(name, out IOpenApiSchema? existing))
            {
                if (!ComponentsEqual(existing, schema))
                {
                    _logger.LogWarning("Schema '{Schema}' from cluster '{ClusterId}' differs from an already-merged schema of the same name; keeping the first. Enable RenameDuplicateSchemas to keep both.", name, clusterId);
                }

                continue;
            }

            merged.Components.Schemas[name] = schema;
        }
    }

    private void MergeSecuritySchemes(OpenApiDocument merged, OpenApiDocument source, string clusterId)
    {
        if (source.Components?.SecuritySchemes is not { } schemes)
        {
            return;
        }

        foreach ((string name, IOpenApiSecurityScheme scheme) in schemes)
        {
            if (merged.Components!.SecuritySchemes!.TryGetValue(name, out IOpenApiSecurityScheme? existing))
            {
                if (!ComponentsEqual(existing, scheme))
                {
                    _logger.LogWarning("Security scheme '{Name}' from cluster '{ClusterId}' differs from an already-merged scheme of the same name; keeping the first.", name, clusterId);
                }

                continue;
            }

            merged.Components.SecuritySchemes[name] = scheme;
        }
    }

    private static bool ComponentsEqual(IOpenApiSerializable left, IOpenApiSerializable right)
        => string.Equals(OpenApiSerializer.SerializeToJson(left), OpenApiSerializer.SerializeToJson(right), StringComparison.Ordinal);

    /// <summary>
    /// Returns a copy of <paramref name="document"/> in which every component schema whose name
    /// already exists in <paramref name="merged"/> with different content is renamed (and all of the
    /// document's <c>$ref</c>s to it rewritten), so it can be merged without losing information.
    /// </summary>
    private OpenApiDocument RenameConflictingSchemas(OpenApiDocument document, OpenApiDocument merged, string clusterId)
    {
        if (document.Components?.Schemas is not { Count: > 0 } schemas)
        {
            return document;
        }

        var taken = new HashSet<string>(merged.Components!.Schemas!.Keys, StringComparer.Ordinal);
        foreach (string name in schemas.Keys)
        {
            taken.Add(name);
        }

        var renames = new Dictionary<string, string>(StringComparer.Ordinal);
        string prefix = ClusterPrefix(clusterId);

        foreach ((string name, IOpenApiSchema schema) in schemas)
        {
            if (merged.Components.Schemas.TryGetValue(name, out IOpenApiSchema? existing) && !ComponentsEqual(existing, schema))
            {
                string newName = MakeUniqueName(taken, prefix, name);
                taken.Add(newName);
                renames[name] = newName;
                _logger.LogInformation("Schema '{Schema}' from cluster '{ClusterId}' conflicts with an existing schema; renaming it to '{NewName}' in the merged document.", name, clusterId, newName);
            }
        }

        return renames.Count == 0 ? document : ApplyRenames(document, renames);
    }

    private static OpenApiDocument ApplyRenames(OpenApiDocument document, IReadOnlyDictionary<string, string> renames)
    {
        JsonNode root = JsonNode.Parse(OpenApiSerializer.SerializeToJson(document))!;

        if (root["components"]?["schemas"] is JsonObject schemaObject)
        {
            foreach ((string oldName, string newName) in renames)
            {
                if (schemaObject.TryGetPropertyValue(oldName, out JsonNode? node) && node is not null)
                {
                    schemaObject.Remove(oldName);
                    schemaObject[newName] = node.DeepClone();
                }
            }
        }

        RewriteSchemaRefs(root, renames);

        return OpenApiSerializer.Parse(root.ToJsonString()).Document!;
    }

    private static void RewriteSchemaRefs(JsonNode? node, IReadOnlyDictionary<string, string> renames)
    {
        switch (node)
        {
            case JsonObject obj:
                if (obj.TryGetPropertyValue("$ref", out JsonNode? refNode)
                    && refNode is JsonValue refValue
                    && refValue.TryGetValue(out string? reference)
                    && reference.StartsWith(SchemaRefPrefix, StringComparison.Ordinal)
                    && renames.TryGetValue(reference[SchemaRefPrefix.Length..], out string? renamed))
                {
                    obj["$ref"] = SchemaRefPrefix + renamed;
                }

                foreach (KeyValuePair<string, JsonNode?> property in obj.ToList())
                {
                    RewriteSchemaRefs(property.Value, renames);
                }

                break;

            case JsonArray array:
                foreach (JsonNode? element in array.ToList())
                {
                    RewriteSchemaRefs(element, renames);
                }

                break;
        }
    }

    private static string MakeUniqueName(HashSet<string> taken, string prefix, string name)
    {
        string candidate = prefix + name;
        int suffix = 2;
        while (taken.Contains(candidate))
        {
            candidate = $"{prefix}{name}{suffix++}";
        }

        return candidate;
    }

    private static string ClusterPrefix(string clusterId)
    {
        var builder = new StringBuilder(clusterId.Length);
        bool upperNext = true;
        foreach (char c in clusterId)
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(upperNext ? char.ToUpperInvariant(c) : c);
                upperNext = false;
            }
            else
            {
                upperNext = true;
            }
        }

        return builder.Length == 0 ? "Service" : builder.ToString();
    }
}
